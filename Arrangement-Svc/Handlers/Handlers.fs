module Handlers

open Giraffe
open System
open System.Web
open Thoth.Json.Net
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Http

open Email.Service

open Auth
open Config
open Models
open Database
open UserMessage
open UserMessage.ResponseMessages

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
                let! userId = getUserId context
                
                let! writeModel =
                    decode ParticipantWriteModel.decoder context
                    |> TaskResult.mapError BadRequest
                
                let config = context.GetService<AppConfig>()
                
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! eventAndQuestions =
                    Queries.getEvent isBekker eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! numberOfParticipants =
                    Queries.getNumberOfParticipantsForEvent isBekker eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                
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
                        let participant = Queries.addParticipantToEvent eventId email userId writeModel.Name transaction
                        let answers =
                            if List.isEmpty writeModel.ParticipantAnswers then
                                Ok []
                            else
                                // FIXME: Here we need to fetch all the questions from the database. This is because we have no question ID related to the answers. This does not feel right and should be fixed. Does require a frontend fix as well
                                let eventQuestions = Queries.getEventQuestions eventId transaction
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
                                Queries.createParticipantAnswers participantAnswerDbModels transaction
                        Ok (participant, answers)
                    result |> Result.mapError (internal_error_and_rollback_transaction transaction)
                let! participant = participant |> Result.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
                let! answers = answers |> Result.mapError (internal_error_and_rollback_transaction transaction)
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
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! events =
                    Queries.getEventsForForside email transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
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
                    |> TaskResult.requireSome couldNotRetrieveUserId
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! eventAndQuestions =
                    Queries.getFutureEvents userId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
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
                    |> TaskResult.requireSome couldNotRetrieveUserId
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! eventAndQuestions =
                    Queries.getPastEvents userId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
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
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! eventAndQuestions =
                    Queries.getEventsOrganizedByEmail email transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
                return
                    eventAndQuestions
                    |> List.map Event.encodeEventAndQuestions
            }
        jsonResult result next context
        
let getEventIdByShortname =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let! shortnameEncoded =
                    context.GetQueryStringValue "shortname"
                    |> Result.mapError BadRequest
                let shortname = HttpUtility.UrlDecode(shortnameEncoded)
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! result =
                    Queries.getEventIdByShortname shortname transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
                return result
            }
        jsonResult result next context       
    
let getEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let isBekker = context.User.Identity.IsAuthenticated
        let result =
            taskResult {
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! eventAndQuestions =
                    Queries.getEvent isBekker eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                transaction.Commit()
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
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                // TODO: USikker på hvilken av disse som er riktig.
                // Gamle versjon gjør det på utkommentert måte, men den funker ikke i postman
//                let success, parsedEventId = Guid.TryParse (idOrName |> strSkip ("/events/" |> String.length))
                let success, parsedEventId = Guid.TryParse idOrName 
                let! eventId =
                    if not success then
                        let name = idOrName |> strSkip 1
                        taskResult {
                            let! result =
                                Queries.getEventIdByShortname name transaction
                                |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                            return result
                        }
                    else
                       task {
                           return Ok parsedEventId
                       }
                       
                let! eventAndQuestions =
                    Queries.getEvent true eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! numberOfParticipants =
                    Queries.getNumberOfParticipantsForEvent true eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
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
                    |> TaskResult.requireSome couldNotRetrieveUserId
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! doesShortNameExist =
                    Queries.doesShortnameExist writeModel.Shortname transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                do! doesShortNameExist
                    |> Result.requireFalse (shortnameIsInUse writeModel.Shortname)
                let! newEvent =
                    Queries.createEvent writeModel userId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! newQuestions =
                    Queries.createParticipantQuestions newEvent.Id writeModel.ParticipantQuestions transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
                let eventAndQuestions = { Event = newEvent; Questions = newQuestions }
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
                let! userId = getUserId context
                let userIsAdmin = isAdmin context
                let editToken = getEditTokenFromQuery context
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! canEditEvent =
                    Queries.canEditEvent eventId userIsAdmin userId editToken transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                do! canEditEvent |> Result.requireTrue cannotUpdateEvent
                do! Queries.cancelEvent eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! eventAndQuestions =
                    Queries.getEvent true eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participants =
                    Queries.getParticipantsForEvent eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
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
                let! userId = getUserId context
                let userIsAdmin = isAdmin context
                let editToken = getEditTokenFromQuery context
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! canEditEvent =
                    Queries.canEditEvent eventId userIsAdmin userId editToken transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                do! canEditEvent |> Result.requireTrue cannotUpdateEvent
                let! eventAndQuestions =
                    Queries.getEvent true eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participants =
                    Queries.getParticipantsForEvent eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                do! Queries.deleteEvent eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
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
                    |> TaskResult.requireSome couldNotRetrieveUserId
                let userIsAdmin = isAdmin context
                do! (userId = id || userIsAdmin)
                    |> Result.requireTrue cannotSeeParticipations
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! eventsAndQuestions =
                    Queries.getEventsOrganizedById id transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! participations = Queries.getParticipationsById id transaction
                                      |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
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
    
let updateEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let! userId = getUserId context
                let userIsAdmin = isAdmin context
                let editToken = getEditTokenFromQuery context
                let! writeModel =
                    decodeWriteModel<Models.EventWriteModel> context
                    |> TaskResult.mapError BadRequest
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! canEditEvent =
                    Queries.canEditEvent eventId userIsAdmin userId editToken transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                do! canEditEvent |> Result.requireTrue cannotUpdateEvent
                let! oldEvent =
                    Queries.getEvent true eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! oldEventParticipants =
                    Queries.getParticipantsForEvent eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! oldEvent =
                    oldEvent
                    |> Result.requireSome (eventNotFound eventId)
                let! numberOfParticipantsForOldEvent =
                    Queries.getNumberOfParticipantsForEvent true eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                    
                do! canUpdateNumberOfParticipants oldEvent.Event writeModel numberOfParticipantsForOldEvent
                    |> Result.mapError id
                    
                let canUpdateQuestions = canUpdateQuestions writeModel.ParticipantQuestions oldEvent.Questions numberOfParticipantsForOldEvent
                    
                if canUpdateQuestions.Error then
                    return! TaskResult.error illegalQuestionsUpdate
                else 
                    let mutable eventQuestions = oldEvent.Questions
                        
                    if canUpdateQuestions.ShouldChangeQuestions then
                        let! _ =
                            Queries.deleteParticipantQuestions eventId transaction
                            |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                        let! newQuestions =
                            Queries.createParticipantQuestions eventId writeModel.ParticipantQuestions transaction
                            |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                        eventQuestions <- newQuestions
                        ()
                    
                    let! updatedEvent =
                        Queries.updateEvent eventId writeModel transaction
                        |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                    transaction.Commit()
                    sendEmailToNewParticipants oldEvent.Event.MaxParticipants writeModel.MaxParticipants oldEventParticipants updatedEvent context
                    let eventAndQuestions = { Event = updatedEvent; Questions = eventQuestions }
                    return Event.encodeEventAndQuestions eventAndQuestions
            }
        jsonResult result next context
        
let getNumberOfParticipantsForEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let isBekker = context.User.Identity.IsAuthenticated
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! isEventExternal =
                    Queries.isEventExternal eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                do! (isEventExternal || isBekker) |> Result.requireTrue cannotUpdateEvent
                let! result =
                    Queries.getNumberOfParticipantsForEvent isBekker eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit();
                return result
            }
        jsonResult result next context

let getParticipantsForEvent (eventId: Guid) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! participations =
                    Queries.getParticipantsAndAnswersForEvent eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! event =
                    Queries.getEvent true eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! event = event |> Result.requireSome (eventNotFound eventId)
                transaction.Commit()
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
        builder.Append($"{participant.Name}, {participant.Email}, {answers}\n") |> ignore
    
    let builder = System.Text.StringBuilder()
    
    let questions =
        questions
        |> List.map (fun q -> q.Question)
        |> String.concat ","
        
    builder.Append($"{event.Title}\n") |> ignore
    builder.Append("Påmeldte\n") |> ignore
    builder.Append($"Navn,Epost,{questions}\n") |> ignore
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
                let! userId = getUserId context
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! canEditEvent =
                    Queries.canEditEvent eventId isAdmin userId editToken transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                do! canEditEvent |> Result.requireTrue cannotUpdateEvent
                let! eventAndQuestions =
                    Queries.getEvent true eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participations =
                    Queries.getParticipantsAndAnswersForEvent eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                transaction.Commit()
                let participants = participationsToAttendeesAndWaitlist eventAndQuestions.Event.MaxParticipants (participations |> Seq.toList)
                return createCsvString eventAndQuestions.Event eventAndQuestions.Questions participants 
            }
        csvResult eventId result next context
        
let getWaitinglistSpot (eventId: Guid) (email: string) =
    fun (next: HttpFunc) (context: HttpContext) ->
        let result =
            taskResult {
                let isBekker = context.User.Identity.IsAuthenticated
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! isEventExternal =
                    Queries.isEventExternal eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                do! (isEventExternal || isBekker) |> Result.requireTrue cannotUpdateEvent
                let! eventAndQuestions =
                    Queries.getEvent isBekker eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participations =
                    Queries.getParticipantsAndAnswersForEvent eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
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
                let connection = context.GetService<DatabaseConnection>().getConnection()
                let! result =
                    Queries.getParticipationsForParticipant email connection
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
                let connection = context.GetService<DatabaseConnection>().getConnection()
                use transaction = connection.BeginTransaction()
                let! participant =
                    Queries.getParticipantForEvent eventId email transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                do! (isAdmin || (cancellationToken.IsSome && cancellationToken.Value = participant.CancellationToken))
                    |> Result.requireTrue cannotDeleteParticipation
                let! eventAndQuestions =
                    Queries.getEvent true eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! eventAndQuestions =
                    eventAndQuestions
                    |> Result.requireSome (eventNotFound eventId)
                let! participants =
                    Queries.getParticipantsAndAnswersForEvent eventId transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let! deletedParticipant =
                    Queries.deleteParticipantFromEvent eventId email transaction
                    |> TaskResult.mapError (internal_error_and_rollback_transaction transaction)
                let participants = participationsToAttendeesAndWaitlist eventAndQuestions.Event.MaxParticipants (participants |> Seq.toList)
                transaction.Commit()
                sendParticipantCancelMails eventAndQuestions.Event deletedParticipant participants.WaitingList context
                return ()
            }
        jsonResult result next context
        
    
let routes: HttpHandler =
    choose
        [ POST
          >=> choose [
              routef "/events/%O/participants/%s" registerParticipationHandler
              isAuthenticated >=> choose [
                  route "/events" >=> createEvent
              ]
          ]
          PUT
          >=> choose [
              routef "/events/%O" updateEvent
          ]
          GET
          >=> choose [
            route "/events/id" >=> getEventIdByShortname
            routef "/events/%O" getEvent
            routef "/events/%s/unfurl" getUnfurlEvent
            routef "/events/%O/participants/count" getNumberOfParticipantsForEvent
            routef "/events/%O/participants/%s/waitinglist-spot" (fun (eventId, email) -> getWaitinglistSpot eventId email)
            routef "/events/%O/participants/export" exportParticipationsForEvent
            isAuthenticated >=> choose [
                route "/events" >=> getFutureEvents 
                route "/events/previous" >=> getPastEvents
                routef "/events/forside/%s" getEventsForForsideHandler
                routef "/events/organizer/%s" getEventsOrganizedBy
                routef "/events-and-participations/%i" getEventsAndParticipations
                routef "/events/%O/participants" getParticipantsForEvent
                routef "/participants/%s/events" getParticipationsForParticipant
            ]
          ]
          DELETE
          >=> choose [
              routef "/events/%O" cancelEvent
              routef "/events/%O/delete" deleteEvent
              routef "/events/%O/participants/%s" (fun (eventId, email) -> deleteParticipantFromEvent eventId email)
          ]
        ]