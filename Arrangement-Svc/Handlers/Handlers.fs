module Handlers

open Giraffe
open System
open System.Web
open Thoth.Json.Net
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Http

open Email.Service
open Email.Models

open Auth
open Config
open Models
open UserMessage
open UserMessage.ResponseMessages
open Middleware

let private decodeWriteModel<'T> (context: HttpContext) =
    task {
        let! body = context.ReadBodyFromRequestAsync()
        let result = Decode.Auto.fromString<'T> (body, caseStrategy = CamelCase)
        return result
    }

let private decode decoder (context: HttpContext) =
    task {
        let! body = context.ReadBodyFromRequestAsync()
        let result = Decode.fromString decoder body
        return result
    }

let private createEditUrl (redirectUrlTemplate: string) (event: Models.Event) =
    redirectUrlTemplate.Replace("{eventId}", event.Id.ToString())
                       .Replace("{editToken}", event.EditToken.ToString())

let private createCancelUrl (redirectUrlTemplate: string) (participant: Participant) =
    redirectUrlTemplate.Replace("{eventId}",
                            participant.EventId.ToString
                                ())
                   .Replace("{email}",
                            participant.Email
                            |> Uri.EscapeDataString)
                   .Replace("{cancellationToken}",
                            participant.CancellationToken.ToString
                                ())

let private participationsToAttendeesAndWaitlist maxParticipants participations =
    match maxParticipants with
    | None ->
        { Attendees = participations
          WaitingList = [] }
    | Some max ->
        { Attendees =
                participations
                |> List.truncate max
          WaitingList =
               participations
               |> Seq.safeSkip max
               |> Seq.toList }

let getEditTokenFromQuery (context: HttpContext) =
    context.GetQueryStringValue "editToken"
    |> function
        | Ok editToken -> Guid.Parse(editToken)
        | _ -> Guid.Empty
let getCancellationTokenFromQuery (context: HttpContext) =
    context.GetQueryStringValue "cancellationToken"
    |> Result.map Guid.TryParse
    |> function
        | Ok (_, r) -> Some r
        | Error _ -> None

type private ParticipateEvent =
    | NotExternal
    | IsCancelled
    | NotOpenForRegistration
    | HasAlreadyTakenPlace
    | NoRoom
    | IsWaitListed
    | CanParticipate

let private participateEvent isBekker numberOfParticipants (event: Models.Event) =
    let currentEpoch = DateTimeOffset.Now.ToUnixTimeMilliseconds()
    let hasRoom = event.MaxParticipants.IsNone || event.MaxParticipants.IsSome && event.MaxParticipants.Value > numberOfParticipants
    // Eventet er ikke ekstern
    // Brukeren er ikke en bekker
    if not event.IsExternal && not isBekker then
        NotExternal
    // Eventet er kansellert
    else if event.IsCancelled then
        IsCancelled
    // Eventet er ikke åpent for registrering
    else if event.OpenForRegistrationTime >= currentEpoch || (event.CloseRegistrationTime.IsSome && event.CloseRegistrationTime.Value < currentEpoch) then
        NotOpenForRegistration
    // Eventet har funnet sted
    else if DateTimeCustom.now() > (DateTimeCustom.toCustomDateTime event.EndDate event.EndTime) then
        HasAlreadyTakenPlace
    // Eventet har ikke nok ledig plass
    else if not hasRoom && not event.HasWaitingList then
        NoRoom
    else if not hasRoom && event.HasWaitingList then
        IsWaitListed
    else
        CanParticipate

let registerParticipationHandler (eventId: Guid, email): HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let isBekker = context.User.Identity.IsAuthenticated
                let userId = getUserId context

                let! writeModel =
                    decode ParticipantWriteModel.decoder context
                    |> TaskResult.mapError BadRequest

                let config = context.GetService<AppConfig>()

                use db = openTransaction context
                let! isEventExternal =
                    Queries.isEventExternal eventId db
                    |> TaskResult.mapError InternalError
                do! (isBekker || isEventExternal) |> Result.requireTrue mustBeAuthorizedOrEventMustBeExternal
                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! numberOfParticipants =
                    Queries.getNumberOfParticipantsForEvent eventId db
                    |> TaskResult.mapError InternalError

                let! participate =
                    match participateEvent isBekker numberOfParticipants eventAndQuestions.Event with
                        | NotExternal ->
                            Error "Arrangementet er ikke eksternt"
                        | IsCancelled ->
                            Error "Arrangementet er kansellert"
                        | NotOpenForRegistration ->
                            Error "Arrangementet er ikke åpent for registering"
                        | HasAlreadyTakenPlace ->
                            Error "Arrangementet tok sted i fortiden"
                        | NoRoom ->
                            Error "Arrangementet har ikke plass"
                        | IsWaitListed ->
                            Ok IsWaitListed
                        | CanParticipate ->
                            Ok CanParticipate
                    |> Result.mapError (fun e -> BadRequest e)

                let! participant, answers =
                    let result =
                        let participant = Queries.addParticipantToEvent eventId email userId writeModel.Name writeModel.Department db
                        let answers =
                            if List.isEmpty writeModel.ParticipantAnswers then
                                Ok []
                            else
                                // FIXME: Here we need to fetch all the questions from the database. This is because we have no question ID related to the answers. This does not feel right and should be fixed. Does require a frontend fix as well
                                let eventQuestions = Queries.getEventQuestions eventId db
                                let participantAnswerDbModels: ParticipantAnswer list =
                                    writeModel.ParticipantAnswers
                                    |> Seq.zip eventQuestions
                                    |> Seq.map (fun (question, answer) ->
                                        { QuestionId = question.Id
                                          EventId = eventId
                                          Email = email
                                          Answer = answer
                                        })
                                    |> Seq.toList
                                Queries.createParticipantAnswers participantAnswerDbModels db
                        Ok (participant, answers)
                    result |> Result.mapError InternalError
                let! participant = participant |> Result.mapError InternalError
                let! answers = answers |> Result.mapError InternalError
                db.Commit()
                // Sende epost
                let isWaitlisted = participate = IsWaitListed
                let email =
                    let redirectUrlTemplate =
                        HttpUtility.UrlDecode writeModel.CancelUrlTemplate

                    createNewParticipantMail
                        (createCancelUrl redirectUrlTemplate) eventAndQuestions.Event isWaitlisted
                        config.noReplyEmail
                        participant
                sendMail email context

                return Participant.encodeWithCancelInfo participant answers
            }
        jsonResult result next context

let getEventsForForsideHandler (email: string) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                use db = openConnection context
                let! events =
                    Queries.getEventsForForside email db
                    |> TaskResult.mapError InternalError
                return events
                       |> Seq.map Event.encodeForside
                       |> Encode.seq
            }
        jsonResult result next context

let getFutureEvents (next: HttpFunc) (context: HttpContext) =
    let result =
        taskResult {
            let! userId =
                getUserId context
                |> Result.requireSome couldNotRetrieveUserId
            use db = openConnection context
            let! eventAndQuestions =
                Queries.getFutureEvents userId db
                |> TaskResult.mapError InternalError
            let result =
                eventAndQuestions
                |> List.map Event.encodeEventAndQuestions
            return result
        }
    jsonResult result next context

let getPastEvents (next: HttpFunc) (context: HttpContext) =
        let result =
            taskResult {
                let! userId =
                    getUserId context
                    |> Result.requireSome couldNotRetrieveUserId
                use db = openConnection context
                let! eventAndQuestions =
                    Queries.getPastEvents userId db
                    |> TaskResult.mapError InternalError
                return
                    eventAndQuestions
                    |> List.map Event.encodeEventAndQuestions
                    |> Encode.list
            }
        jsonResult result next context

let getEventsOrganizedBy (email: string) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                use db = openConnection context
                let! eventAndQuestions =
                    Queries.getEventsOrganizedByEmail email db
                    |> TaskResult.mapError InternalError
                return
                    eventAndQuestions
                    |> List.map Event.encodeEventAndQuestions
            }
        jsonResult result next context

let getEventIdByShortnameHttpResult shortname db =
    taskResult {
        let! result =
            Queries.getEventIdByShortname shortname db
            |> TaskResult.mapError InternalError
        return! Result.requireSome (NotFound $"Kunne ikke finne event med kortnavn %s{shortname}") result
    }

let getEventIdByShortname =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let! shortnameEncoded =
                    context.GetQueryStringValue "shortname"
                    |> Result.mapError BadRequest
                let shortname = HttpUtility.UrlDecode(shortnameEncoded)
                use db = openConnection context
                return! getEventIdByShortnameHttpResult shortname db
            }
        jsonResult result next context

let getEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let isBekker = context.User.Identity.IsAuthenticated
        let result =
            taskResult {
                use db = openConnection context
                let! isEventExternal =
                    Queries.isEventExternal eventId db
                    |> TaskResult.mapError InternalError
                do! (isBekker || isEventExternal) |> Result.requireTrue mustBeAuthorizedOrEventMustBeExternal
                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                return Event.encodeEventAndQuestions eventAndQuestions
            }
        jsonResult result next context

let getUnfurlEvent (idOrName: string) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let strSkip n (s: string) =
            s
            |> Seq.safeSkip n
            |> Seq.map string
            |> String.concat ""
        let result =
            taskResult {
                use db = openConnection context
                // TODO: USikker på hvilken av disse som er riktig.
                // Gamle versjon gjør det på utkommentert måte, men den funker ikke i postman
//                let success, parsedEventId = Guid.TryParse (idOrName |> strSkip ("/events/" |> String.length))
                let! eventId =
                    match Guid.TryParse idOrName with
                    | true, guid ->
                        TaskResult.ok guid
                    | false, _ ->
                        let name = idOrName |> strSkip 1
                        getEventIdByShortnameHttpResult name db

                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! numberOfParticipants =
                    Queries.getNumberOfParticipantsForEvent eventId db
                    |> TaskResult.mapError InternalError
                return {| event = Event.encodeEventAndQuestions eventAndQuestions; numberOfParticipants = numberOfParticipants |}
            }
        jsonResult result next context

let createEvent =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let! writeModel =
                    decode EventWriteModel.decoder context
                    |> TaskResult.mapError BadRequest
                let! userId =
                    getUserId context
                    |> Result.requireSome couldNotRetrieveUserId
                use db = openTransaction context
                let! doesShortNameExist =
                    Queries.doesShortnameExist writeModel.Shortname db
                    |> TaskResult.mapError InternalError
                do! doesShortNameExist
                    |> Result.requireFalse (shortnameIsInUse writeModel.Shortname)
                let! newEvent =
                    Queries.createEvent writeModel userId db
                    |> TaskResult.mapError InternalError
                let! newQuestions =
                    Queries.createParticipantQuestions newEvent.Id writeModel.ParticipantQuestions db
                    |> TaskResult.mapError InternalError
                db.Commit()
                let eventAndQuestions = { Event = newEvent; NumberOfParticipants = None; Questions = newQuestions }
                // Send epost etter registrering
                let redirectUrlTemplate = HttpUtility.UrlDecode writeModel.EditUrlTemplate
                let viewUrl = writeModel.ViewUrl
                sendNewlyCreatedEventMail viewUrl (createEditUrl redirectUrlTemplate) newEvent context
                return Event.encoderWithEditInfo eventAndQuestions
            }
        jsonResult result next context

let cancelEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let config = context.GetService<AppConfig>()
                let userId = getUserId context
                let userIsAdmin = isAdmin context
                let editToken = getEditTokenFromQuery context
                use db = openTransaction context
                let! canEditEvent =
                    Queries.canEditEvent eventId userIsAdmin userId editToken db
                    |> TaskResult.mapError InternalError
                do! canEditEvent |> Result.requireTrue cannotUpdateEvent
                do! Queries.cancelEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participants =
                    Queries.getParticipantsForEvent eventId db
                    |> TaskResult.mapError InternalError
                db.Commit()
                let! messageToParticipants = context.ReadBodyFromRequestAsync()
                sendCancellationMailToParticipants
                    messageToParticipants
                    config.noReplyEmail
                    participants
                    eventAndQuestions.Event
                    context
                return eventSuccessfullyCancelled eventAndQuestions.Event.Title
            }
        jsonResult result next context

let deleteEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let config = context.GetService<AppConfig>()
                let userId = getUserId context
                let userIsAdmin = isAdmin context
                let editToken = getEditTokenFromQuery context
                use db = openTransaction context
                let! canEditEvent =
                    Queries.canEditEvent eventId userIsAdmin userId editToken db
                    |> TaskResult.mapError InternalError
                do! canEditEvent |> Result.requireTrue cannotUpdateEvent
                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participants =
                    Queries.getParticipantsForEvent eventId db
                    |> TaskResult.mapError InternalError
                do! Queries.deleteEvent eventId db
                    |> TaskResult.mapError InternalError
                db.Commit()
                let! messageToParticipants = context.ReadBodyFromRequestAsync()
                sendCancellationMailToParticipants
                    messageToParticipants
                    config.noReplyEmail
                    participants
                    eventAndQuestions.Event
                    context
                return eventSuccessfullyCancelled eventAndQuestions.Event.Title
            }
        jsonResult result next context

let getEventsAndParticipations (id: int) =
       fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let! userId =
                    getUserId context
                    |> Result.requireSome couldNotRetrieveUserId
                let userIsAdmin = isAdmin context
                do! (userId = id || userIsAdmin)
                    |> Result.requireTrue cannotSeeParticipations
                use db = openConnection context
                let! eventsAndQuestions =
                    Queries.getEventsOrganizedById id db
                    |> TaskResult.mapError InternalError
                let! participations = Queries.getParticipationsById id db
                                      |> TaskResult.mapError InternalError
                return Participant.encodeWithLocalStorage eventsAndQuestions (participations |> Seq.toList)
            }
        jsonResult result next context

let private canUpdateNumberOfParticipants (oldEvent: Models.Event) (newEvent: Models.EventWriteModel) oldEventParticipants =
    match oldEvent.MaxParticipants, newEvent.MaxParticipants with
    | _, None ->
        // Den nye er uendelig, all good
        Ok ()
    | None, Some newMax ->
        // Det er plass til alle
        if newMax >= oldEventParticipants then
            Ok ()
        else
            Error invalidMaxParticipantValue
    | Some oldMax, Some newMax ->
        // Man kan alltid øke så lenge den forrige
        // ikke var uendelig
        // og man har ventelist
        if newMax >= oldMax && newEvent.HasWaitingList then
            Ok ()
        // Det er plass til alle
        else if newMax >= oldEventParticipants then
            Ok ()
        else
            // Dette er ikke lov her nede fordi vi sjekker over at
            // det ikke er plass til alle
            // Derfor vil det være frekt å fjerne ventelista
            let isRemovingWaitlist = newEvent.HasWaitingList = false && oldEvent.HasWaitingList = true
            if isRemovingWaitlist then
                Error invalidRemovalOfWaitingList
            else
                Error invalidMaxParticipantValue

let private canUpdateQuestions newEventQuestions (oldEventQuestions: ParticipantQuestion list) oldEventParticipants =
    let newEventQuestions =
        newEventQuestions
        |> List.sort

    let oldEventQuestions =
        oldEventQuestions
        |> List.map (fun question -> question.Question)
        |> List.sort

    let shouldChangeQuestions = newEventQuestions <> oldEventQuestions
    let canChangeQuestions = shouldChangeQuestions && oldEventParticipants = 0

    {| ShouldChangeQuestions = shouldChangeQuestions; Error = shouldChangeQuestions && not canChangeQuestions |}

let private sendEmailToNewParticipants oldEventMaxParticipants newEventMaxParticipants (oldEventParticipants: Models.Participant seq)updatedEvent context =
    let oldEventWaitlist =
        match oldEventMaxParticipants with
        | None -> Seq.empty
        | Some maxParticipants -> Seq.safeSkip maxParticipants oldEventParticipants
    let numberOfPeople =
        match oldEventMaxParticipants, newEventMaxParticipants with
        | Some _, None -> Seq.length oldEventWaitlist
        | Some old, Some new' -> new' - old
        | _ -> 0

    if numberOfPeople > 0 then
        let newPeople =
            oldEventWaitlist
            |> Seq.truncate numberOfPeople
        for newAttendee in newPeople do
            sendMail (createFreeSpotAvailableMail updatedEvent newAttendee) context

let diffEvent (oldEvent: Event) (newEvent : Event) =
    let extract (ev : Event) =
        {| start = DateTimeCustom.toCustomDateTime ev.StartDate ev.StartTime
           end' = DateTimeCustom.toCustomDateTime ev.EndDate ev.EndTime
           location = ev.Location |}
    let old' = extract oldEvent
    let new' = extract newEvent
    if old' <> new' then
        Some {| oldStart = old'.start
                oldEnd = old'.end'
                newStart = new'.start
                newEnd = new'.end'
                oldLocation = old'.location
                newLocation = new'.location |}
    else None

let sendUpdateEmailToOldParticipants (old' : Event) (new' : Event) (oldParticipants : Participant seq) (cancelTemplate: string option) (ctx : HttpContext) =
    diffEvent old' new'
    |> Option.iter (fun diff ->
        let cfg = ctx.GetService<AppConfig>()
        oldParticipants
        |> Seq.map (fun (p : Participant) ->
            let diffMsg =
                  let timeChanged = DateTimeCustom.toReadableDiff diff.oldStart diff.oldEnd diff.newStart diff.newEnd
                  let locationChanged =
                      if diff.oldLocation <> diff.newLocation
                      then Some (diff.oldLocation, diff.newLocation)
                      else None
                  [ $"Hei! 😄"
                    $""
                    match timeChanged, locationChanged with
                    | Some (fromTime, toTime), Some(fromLoc, toLoc) ->
                        $"Arrangementet du har meldt deg på har endret tidspunkt og lokasjon: "
                        $"- Tidspunkt er endret fra {fromTime} til {toTime}"
                        $"- Lokasjon er endret fra {fromLoc} til {toLoc}"
                    | Some (fromTime, toTime), None ->
                        $"Arrangementet du har meldt deg på har endret tidspunkt fra {fromTime} til {toTime}"
                    | None, Some (fromLoc, toLoc) ->
                        $"Arrangementet du har meldt deg på har endret lokasjon fra {fromLoc} til {toLoc}"
                    | None, None ->
                        failwithf "Only expected to send update notification for certain fields, but there are other changes!"
                    $""
                    $"Vi gleder oss til å se deg! 🎉"
                    $""
                    if new'.MaxParticipants.IsSome
                    then "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger<br>kan delta. Da blir det plass til andre på ventelisten 😊"
                    else "Gjerne meld deg av dersom du ikke lenger har mulighet til å delta."
                    if cancelTemplate.IsSome then $"Du kan melde deg av <a href=\"{(createCancelUrl cancelTemplate.Value p)}\">via denne lenken</a>."
                    $""
                    $"Bare send meg en mail på {new'.OrganizerEmail} om det er noe du lurer på."
                    $"Vi sees!"
                    $""
                    $"Hilsen {new'.OrganizerName} i Bekk"
                  ] |> String.concat "<br>"
            { Subject = $"Arrangementet {old'.Title} er endret"
              Message = diffMsg
              To = p.Email
              CalendarInvite =
                  (new', p, cfg.noReplyEmail, diffMsg, Email.CalendarInvite.Update)
                  |> Email.CalendarInvite.createCalendarAttachment
                  |> Some }
        ) |> Seq.iter (fun msg -> sendMail msg ctx))

let updateEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let userId = getUserId context
                let userIsAdmin = isAdmin context
                let editToken = getEditTokenFromQuery context
                let! writeModel =
                    decodeWriteModel<Models.EventWriteModel> context
                    |> TaskResult.mapError BadRequest
                use db = openTransaction context
                let! canEditEvent =
                    Queries.canEditEvent eventId userIsAdmin userId editToken db
                    |> TaskResult.mapError InternalError
                do! canEditEvent |> Result.requireTrue cannotUpdateEvent
                let! oldEvent =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! oldEventParticipants =
                    Queries.getParticipantsForEvent eventId db
                    |> TaskResult.mapError InternalError
                let! oldEvent =
                    oldEvent
                    |> Result.requireSome (eventNotFound eventId)
                let! numberOfParticipantsForOldEvent =
                    Queries.getNumberOfParticipantsForEvent eventId db
                    |> TaskResult.mapError InternalError

                do! canUpdateNumberOfParticipants oldEvent.Event writeModel numberOfParticipantsForOldEvent
                    |> Result.mapError id

                let canUpdateQuestions = canUpdateQuestions writeModel.ParticipantQuestions oldEvent.Questions numberOfParticipantsForOldEvent

                if canUpdateQuestions.Error then
                    return! TaskResult.error illegalQuestionsUpdate
                else
                    let mutable eventQuestions = oldEvent.Questions

                    if canUpdateQuestions.ShouldChangeQuestions then
                        let! _ =
                            Queries.deleteParticipantQuestions eventId db
                            |> TaskResult.mapError InternalError
                        let! newQuestions =
                            Queries.createParticipantQuestions eventId writeModel.ParticipantQuestions db
                            |> TaskResult.mapError InternalError
                        eventQuestions <- newQuestions
                        ()

                    let! updatedEvent =
                        Queries.updateEvent eventId writeModel db
                        |> TaskResult.mapError InternalError
                    db.Commit()
                    sendEmailToNewParticipants oldEvent.Event.MaxParticipants writeModel.MaxParticipants oldEventParticipants updatedEvent context
                    let cancelUrl = writeModel.CancelParticipationUrlTemplate |> Option.map HttpUtility.UrlDecode in
                        sendUpdateEmailToOldParticipants oldEvent.Event updatedEvent oldEventParticipants cancelUrl context
                    let eventAndQuestions = { Event = updatedEvent; NumberOfParticipants = None; Questions = eventQuestions }
                    return Event.encodeEventAndQuestions eventAndQuestions
            }
        jsonResult result next context

let getNumberOfParticipantsForEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let isBekker = context.User.Identity.IsAuthenticated
                use db = openConnection context
                let! isEventExternal =
                    Queries.isEventExternal eventId db
                    |> TaskResult.mapError InternalError
                do! (isEventExternal || isBekker) |> Result.requireTrue cannotUpdateEvent
                let! result =
                    Queries.getNumberOfParticipantsForEvent eventId db
                    |> TaskResult.mapError InternalError
                return result
            }
        jsonResult result next context

let getParticipantsForEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                use db = openConnection context
                let! participations =
                    Queries.getParticipantsAndAnswersForEvent eventId db
                    |> TaskResult.mapError InternalError
                let! event =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! event = event |> Result.requireSome (eventNotFound eventId)
                return Participant.encodeParticipationsAndWaitlist (participationsToAttendeesAndWaitlist event.Event.MaxParticipants (participations |> Seq.toList))
            }
        jsonResult result next context

let createCsvString (event: Models.Event) (questions: ParticipantQuestion list) (participants: ParticipationsAndWaitlist) =
    let createParticipant (builder: System.Text.StringBuilder) (participantAndAnswers: ParticipantAndAnswers) =
        let participant = participantAndAnswers.Participant
        let answers = participantAndAnswers.Answers
        let answers =
            answers
            |> List.map (fun a -> $"{a.Answer}")
            |> String.concat ","
        let employeeId =
            participant.EmployeeId
            |> Option.map string
            |> Option.defaultValue ""
        let department =
            participant.Department
            |> Option.defaultValue ""
        builder.Append($"{employeeId}, {participant.Name}, {participant.Email}, {department}, {answers}\n") |> ignore

    let builder = System.Text.StringBuilder()

    let questions =
        questions
        |> List.map (fun q -> q.Question)
        |> String.concat ","

    builder.Append($"{event.Title}\n") |> ignore
    builder.Append("Påmeldte\n") |> ignore
    builder.Append($"AnsattId,Navn,Epost,Avdeling,{questions}\n") |> ignore
    Seq.iter (createParticipant builder) participants.Attendees
    if not <| Seq.isEmpty participants.WaitingList then
        builder.Append("Venteliste\n") |> ignore
        Seq.iter (createParticipant builder) participants.WaitingList

    builder.ToString()

let exportParticipationsForEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let editToken = getEditTokenFromQuery context
                let isAdmin = isAdmin context
                let userId = getUserId context
                use db = openConnection context
                let! canEditEvent =
                    Queries.canEditEvent eventId isAdmin userId editToken db
                    |> TaskResult.mapError InternalError
                do! canEditEvent |> Result.requireTrue cannotUpdateEvent
                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participations =
                    Queries.getParticipantsAndAnswersForEvent eventId db
                    |> TaskResult.mapError InternalError
                let participants = participationsToAttendeesAndWaitlist eventAndQuestions.Event.MaxParticipants (participations |> Seq.toList)
                return createCsvString eventAndQuestions.Event eventAndQuestions.Questions participants
            }
        csvResult eventId result next context

let getWaitinglistSpot (eventId: Guid) (email: string) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let isBekker = context.User.Identity.IsAuthenticated
                use db = openConnection context
                let! isEventExternal =
                    Queries.isEventExternal eventId db
                    |> TaskResult.mapError InternalError
                do! (isEventExternal || isBekker) |> Result.requireTrue cannotUpdateEvent
                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participations =
                    Queries.getParticipantsAndAnswersForEvent eventId db
                    |> TaskResult.mapError InternalError
                let attendeesAndWaitlist = participationsToAttendeesAndWaitlist eventAndQuestions.Event.MaxParticipants (participations |> Seq.toList)

                let waitingListIndex =
                    attendeesAndWaitlist.WaitingList
                    |> Seq.tryFindIndex (fun participantAndAnswers -> participantAndAnswers.Participant.Email = email)
                    |> Option.map (fun x -> x + 1)
                    |> Option.defaultValue 0

                return waitingListIndex
            }
        jsonResult result next context

let getParticipationsForParticipant (email: string) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                use db = openConnection context
                let! result =
                    Queries.getParticipationsForParticipant email db
                    |> TaskResult.mapError InternalError
                return
                    result
                    |> Seq.map Participant.encodeParticipantAndAnswers
            }
        jsonResult result next context

let deleteParticipantFromEvent (eventId: Guid) (email: string) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let isAdmin = isAdmin context
                let cancellationToken = getCancellationTokenFromQuery context
                use db = openTransaction context
                let! participant =
                    Queries.getParticipantForEvent eventId email db
                    |> TaskResult.mapError InternalError
                do! (isAdmin || (cancellationToken.IsSome && cancellationToken.Value = participant.CancellationToken))
                    |> Result.requireTrue cannotDeleteParticipation
                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participants =
                    Queries.getParticipantsAndAnswersForEvent eventId db
                    |> TaskResult.mapError InternalError
                let participationsAndWaitlist =
                    participants
                    |> Seq.toList
                    |> participationsToAttendeesAndWaitlist eventAndQuestions.Event.MaxParticipants
                let participantIsAttendee =
                    participationsAndWaitlist.Attendees
                    |> Seq.exists (fun x -> x.Participant.Email = email)
                let personWhoGotIt =
                    if participantIsAttendee then
                        List.tryHead participationsAndWaitlist.WaitingList
                    else
                        None
                let! deletedParticipant =
                    Queries.deleteParticipantFromEvent eventId email db
                    |> TaskResult.mapError InternalError
                db.Commit()
                let deletedParticipantAnswers =
                    participants
                    |> Seq.find (fun pa -> pa.Participant.Email = email)
                    |> fun p -> p.Answers
                sendParticipantCancelMails eventAndQuestions.Event eventAndQuestions.Questions deletedParticipant deletedParticipantAnswers personWhoGotIt context
                return ()
            }
        jsonResult result next context

open GiraffeHelpers

let routes: HttpHandler =
    choose
        [
          POST
          >=> choose [
              routef "/api/events/%O/participants/%s" registerParticipationHandler
              // Has authentication
              route "/api/events" >=> isAuthenticated >=> createEvent
          ]
          PUT
          >=> choose [
              routef "/api/events/%O" updateEvent
          ]
          GET
          >=> choose [
            route "/api/events/id" >=> getEventIdByShortname
            routef "/api/events/%O" getEvent
            routef "/api/events/%s/unfurl" getUnfurlEvent
            routef "/api/events/%O/participants/count" getNumberOfParticipantsForEvent
            routef "/api/events/%O/participants/%s/waitinglist-spot" (fun (eventId, email) -> getWaitinglistSpot eventId email)
            routef "/api/events/%O/participants/export" exportParticipationsForEvent
            // Has authentication
            route "/api/events" >=> isAuthenticated >=> getFutureEvents
            route "/api/events/previous" >=> isAuthenticated >=> getPastEvents
            routef "/api/events/forside/%s" (isAuthenticatedf getEventsForForsideHandler)
            routef "/api/events/organizer/%s" (isAuthenticatedf getEventsOrganizedBy)
            routef "/api/events-and-participations/%i" (isAuthenticatedf getEventsAndParticipations)
            routef "/api/events/%O/participants" (isAuthenticatedf getParticipantsForEvent)
            routef "/api/participants/%s/events" (isAuthenticatedf getParticipationsForParticipant)
            routef "/api/office-events/%s" (fun date ->
                isAuthenticated >=>
                outputCache (fun opt -> opt.Duration <- TimeSpan.FromMinutes(5).TotalSeconds)
                >=> OfficeEvents.WebApi.get date
                )
          ]
          DELETE
          >=> choose [
              routef "/api/events/%O" cancelEvent
              routef "/api/events/%O/delete" deleteEvent
              routef "/api/events/%O/participants/%s" (fun (eventId, email) -> deleteParticipantFromEvent eventId email)
          ]
        ]
