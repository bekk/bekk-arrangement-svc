module Handlers

open System.Text
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
        let result = Decode.fromString EventWriteModel.decoder body
        return result
    }

let private decode decoder (context: HttpContext) =
    task {
        let! body = context.ReadBodyFromRequestAsync()
        let result = Decode.fromString decoder body
        return result
    }

let private createViewUrl (viewUrlTemplate: string) (event: Models.Event) =
    let decodedUrlTemplate = HttpUtility.UrlDecode viewUrlTemplate
    decodedUrlTemplate
        .Replace("{shortname}", event.Shortname |> Option.defaultValue "")
        .Replace("{eventId}", event.Id.ToString())

let private createEditUrl (redirectUrlTemplate: string) (event: Models.Event) =
    let decodedUrlTemplate = HttpUtility.UrlDecode redirectUrlTemplate
    decodedUrlTemplate
        .Replace("{eventId}", event.Id.ToString())
        .Replace("{editToken}", event.EditToken.ToString())

let private createCancelUrl (redirectUrlTemplate: string) (participant: Participant) =
    let decodedUrlTemplate = HttpUtility.UrlDecode redirectUrlTemplate
    decodedUrlTemplate
        .Replace("{eventId}", participant.EventId.ToString())
        .Replace("{email}", participant.Email |> Uri.EscapeDataString)
        .Replace("{cancellationToken}", participant.CancellationToken.ToString())

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

let allRequiredQuestionsAnswered (questions: ParticipantQuestion list) (answers: ParticipantAnswer list) =
    questions
    |> List.filter (_.Required)
    |> List.forall (fun q ->
        answers
        |> List.exists (fun a -> a.QuestionId = q.Id && a.Answer.Length > 0)
    )

let registerParticipation (eventId: Guid, email): HttpHandler =
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
                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                do! (isBekker || eventAndQuestions.Event.IsExternal) |> Result.requireTrue mustBeAuthorizedOrEventMustBeExternal
                let numberOfParticipants = Option.defaultValue 0 eventAndQuestions.NumberOfParticipants

                let participationStatus =
                    participateEvent isBekker numberOfParticipants eventAndQuestions.Event
                    
                let! duplicateEmail =
                    Queries.isEmailRegisteredForEvent eventId email db
                    |> TaskResult.mapError InternalError
                do! duplicateEmail
                    |> Result.requireFalse (emailAlreadyRegistered email)
                    
                do! allRequiredQuestionsAnswered eventAndQuestions.Questions writeModel.ParticipantAnswers
                    |> Result.requireTrue unansweredQuestions

                // Verdien blir ignorert da vi nå kun bruker dette til å returnere riktig feil til brukeren.
                // Om arrangemenet har plass eller man er ventelista henter vi ut fra databasen lenger ned.
                let! _ =
                    match participationStatus with
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
                        | IsWaitListed | CanParticipate -> Ok ()
                    |> Result.mapError BadRequest

                let! participant = Queries.addParticipantToEvent eventId email userId writeModel.Name writeModel.Department db
                                   |> Result.mapError InternalError
                let! answers = Queries.createParticipantAnswers writeModel.ParticipantAnswers db
                               |> Result.mapError InternalError
                db.Commit()

                // Sende epost
                let questionAndAnswers = createQuestionAndAnswer eventAndQuestions.Questions answers
                let isWaitlisted = participationStatus = IsWaitListed
                let email =
                    let viewUrl = createViewUrl writeModel.ViewUrlTemplate eventAndQuestions.Event
                    let cancelUrl = createCancelUrl writeModel.CancelUrlTemplate participant

                    createNewParticipantMail
                        viewUrl
                        cancelUrl
                        eventAndQuestions.Event
                        isWaitlisted
                        config.noReplyEmail
                        participant
                        questionAndAnswers

                sendMail email context

                return Participant.encodeWithCancelInfo participant questionAndAnswers
            }
        jsonResult result next context

let getEventsForForside (email: string) =
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

let getOfficeEvents =
    fun (date: string) ->
        outputCache (fun opt -> opt.Duration <- TimeSpan.FromMinutes(5).TotalSeconds)
        >=> OfficeEvents.WebApi.get date
        
let getPublicEvents =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                use db = openConnection context
                let! events =
                    Queries.getPublicEvents db
                    |> TaskResult.mapError InternalError
                    
                let today = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                let! officeEvents =
                    OfficeEvents.WebApi.getAsList today context
                    
                let pastOfficeEvents =
                    List.filter (fun (p: OfficeEvent) -> DateTime.Parse p.StartTime < DateTime.Now) officeEvents
                    |> Seq.sortByDescending (fun x -> DateTime.Parse x.StartTime)
                    |> Seq.truncate 4
                    
                let currentAndFutureOEvents =
                    List.filter (fun (p: OfficeEvent) -> DateTime.Parse p.StartTime >= DateTime.Now) officeEvents
                    |> Seq.sortBy (fun x -> DateTime.Parse x.StartTime)
                    |> Seq.truncate 8
                    
                let encodedEvents =
                   events
                   |> Seq.map Event.encodeSkjerEventSummary
                   |> Encode.seq
                   
                let encodedOfficeEvents =
                   Seq.append pastOfficeEvents currentAndFutureOEvents
                   |> Seq.map Event.encodeOfficeEventSummary
                   |> Encode.seq
                   
                let result = Seq.append encodedEvents encodedOfficeEvents
               
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
                let logger = context.GetService<Bekk.Canonical.Logger.Logger>()
                logger.log ("created_event_with_id", newEvent.Id)
                let eventAndQuestions = { Event = newEvent; NumberOfParticipants = None; Questions = newQuestions }
                // Send epost etter registrering
                let viewUrl = createViewUrl writeModel.ViewUrlTemplate newEvent
                let editUrl = createEditUrl writeModel.EditUrlTemplate newEvent
                sendNewlyCreatedEventMail viewUrl editUrl newEvent context
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

let private canUpdateQuestions (newEventQuestions: ParticipantQuestionWriteModel list) (oldEventQuestions: ParticipantQuestion list) oldEventParticipants =
    let newEventQuestions =
        newEventQuestions
        |> List.map (fun question -> question.Question)
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

let sendUpdateEmailToOldParticipants (old' : Event) (new' : Event) (oldParticipants : Participant seq) (cancelTemplate: string) (ctx : HttpContext) =
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
                    $"Du kan melde deg av <a href=\"{(createCancelUrl cancelTemplate p)}\">via denne lenken</a>."
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
                let numberOfParticipantsForOldEvent = Option.defaultValue 0 oldEvent.NumberOfParticipants

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

                    if writeModel.StartDate > DateTimeCustom.now() then
                        sendEmailToNewParticipants
                            oldEvent.Event.MaxParticipants
                            writeModel.MaxParticipants
                            oldEventParticipants
                            updatedEvent
                            context

                        sendUpdateEmailToOldParticipants
                            oldEvent.Event
                            updatedEvent
                            oldEventParticipants
                            writeModel.CancelParticipationUrlTemplate
                            context

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
                let userId = getUserId context
                let userIsAdmin = isAdmin context
                let editToken = getEditTokenFromQuery context
                let! canEditEvent =
                    Queries.canEditEvent eventId userIsAdmin userId editToken db
                    |> TaskResult.mapError InternalError
                let! participations =
                    Queries.getParticipantsAndAnswersForEvent eventId db
                    |> TaskResult.mapError InternalError
                let! event =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! event = event |> Result.requireSome (eventNotFound eventId)
                return Participant.encodeParticipationsAndWaitlist canEditEvent (participationsToAttendeesAndWaitlist event.Event.MaxParticipants (participations |> Seq.toList)) 
            }
        jsonResult result next context

let createCsvString (event: Models.Event) (questions: ParticipantQuestion list) (participants: ParticipationsAndWaitlist) =
    let formatString (input: string) =
        input.Replace("\n", " ")
        |> sprintf "\"%s\""

    let createParticipant (builder: StringBuilder) (participantAndAnswers: ParticipantAndAnswers) =
        let participant = participantAndAnswers.Participant
        let questionAndAnswers = participantAndAnswers.QuestionAndAnswers
        let answers =
            questionAndAnswers
            |> List.map (fun a -> formatString a.Answer)
            |> String.concat ","
        let employeeId =
            participant.EmployeeId
            |> Option.map string
            |> Option.defaultValue ""
        let department =
            participant.Department
            |> Option.defaultValue ""
        builder.Append($"{employeeId},{participant.Name},{participant.Email},{department},{answers}\n") |> ignore

    let builder = StringBuilder()

    let questions =
        questions
        |> List.map (fun q -> formatString q.Question)
        |> String.concat ","

    builder.Append($"AnsattId,Navn,Epost,Avdeling,{questions}\n") |> ignore
    Seq.iter (createParticipant builder) participants.Attendees

    builder.ToString()

let addUtf8BomToCsv (csvString: string) =
    Array.concat [
        Encoding.UTF8.GetPreamble()
        Encoding.UTF8.GetBytes(csvString)
    ]

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
                let csvString = createCsvString eventAndQuestions.Event eventAndQuestions.Questions participants
                let encodedCsvString = addUtf8BomToCsv csvString
                return encodedCsvString
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

let deleteParticipantFromEvent (eventId: Guid) (email: string) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let isAdmin = isAdmin context
                let cancellationToken = getCancellationTokenFromQuery context
                let editToken = getEditTokenFromQuery context
                use db = openTransaction context
                let! participant =
                    Queries.getParticipantForEvent eventId email db
                    |> TaskResult.mapError InternalError
                let! participant =
                    participant
                    |> Result.requireSome (participantNotFound email eventId)
                let! eventAndQuestions =
                    Queries.getEvent eventId db
                    |> TaskResult.mapError InternalError
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                do! (isAdmin ||
                     (editToken <> Guid.Empty && editToken = eventAndQuestions.Event.EditToken) ||
                     (cancellationToken.IsSome && cancellationToken.Value = participant.CancellationToken))
                    |> Result.requireTrue cannotDeleteParticipation
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
                    |> fun p -> p.QuestionAndAnswers
                sendParticipantCancelMails eventAndQuestions.Event deletedParticipant deletedParticipantAnswers personWhoGotIt participantIsAttendee context
                return ()
            }
        jsonResult result next context

open GiraffeHelpers

let routes: HttpHandler =
    choose
        [
          POST
          >=> choose [
              routef "/api/events/%O/participants/%s" registerParticipation
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
            route "/api/events/public" >=> getPublicEvents
            routef "/api/events/%O" getEvent
            routef "/api/events/%O/participants/count" getNumberOfParticipantsForEvent
            routef "/api/events/%O/participants/%s/waitinglist-spot" (fun (eventId, email) -> getWaitinglistSpot eventId email)
            routef "/api/events/%O/participants/export" exportParticipationsForEvent
            // Has authentication
            route "/api/events" >=> isAuthenticated >=> getFutureEvents
            route "/api/events/previous" >=> isAuthenticated >=> getPastEvents
            routef "/api/events/forside/%s" (isAuthenticatedf getEventsForForside)
            routef "/api/events-and-participations/%i" (isAuthenticatedf getEventsAndParticipations)
            routef "/api/events/%O/participants" (isAuthenticatedf getParticipantsForEvent)
            routef "/api/office-events/%s" (fun date ->
                isAuthenticated >=> getOfficeEvents date)
          ]
          DELETE
          >=> choose [
              routef "/api/events/%O" cancelEvent
              routef "/api/events/%O/delete" deleteEvent
              routef "/api/events/%O/participants/%s" (fun (eventId, email) -> deleteParticipantFromEvent eventId email)
          ]
        ]
