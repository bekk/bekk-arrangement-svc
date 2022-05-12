module V2.Handlers

open Giraffe
open System
open System.Web
open Thoth.Json.Net
open Microsoft.Data.SqlClient
open Microsoft.AspNetCore.Http

open Auth
open Config
open UserMessage
open Participant.Models

type private ParticipateEvent =
    | NotExternal 
    | IsCancelled
    | NotOpenForRegistration
    | HasAlreadyTakenPlace
    | NoRoom
    | IsWaitListed
    | CanParticipate
    
let private participateEvent isBekker numberOfParticipants (event: Event.Models.DbModel) =
    let currentEpoch = DateTimeOffset.Now.ToUnixTimeMilliseconds()
    let hasRoom = event.MaxParticipants.IsNone || event.MaxParticipants.IsSome && numberOfParticipants < event.MaxParticipants.Value
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
        task {
            let isBekker = context.User.Identity.IsAuthenticated
            let userId = Auth.getUserIdV2 context
            
            let! body = context.ReadBodyFromRequestAsync()
            let writeModel = Decode.Auto.fromString<WriteModel> (body, caseStrategy = CamelCase)
            
            let config = context.GetService<AppConfig>()
            
            use connection = new SqlConnection(config.databaseConnectionString)
            connection.Open()
            use transaction = connection.BeginTransaction()
            let event = Queries.getEvent eventId transaction
            let numberOfParticipants = Queries.getNumberOfParticipantsForEvent eventId transaction
            
            let result =
                match event, writeModel with
                | Ok event, Ok writeModel ->
                    let canParticipate = 
                        match participateEvent isBekker numberOfParticipants event with
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
                    match canParticipate with
                    | Error e -> Error e
                    | Ok participate -> 
                        let insertResult =    
                            try
                                let participant = Queries.addParticipantToEvent eventId email userId writeModel.Name transaction
                                let answers =
                                    if List.isEmpty writeModel.ParticipantAnswers then
                                        Ok []
                                    else
                                        // FIXME: Here we need to fetch all the questions from the database. This is because we have no question ID related to the answers. This does not feel right and should be fixed. Does require a frontend fix as well
                                        let eventQuestions = Queries.getEventQuestions eventId transaction
                                        let participantAnswerDbModels: ParticipantAnswerDbModel list =
                                            writeModel.ParticipantAnswers
                                            |> List.zip eventQuestions
                                            |> List.map (fun (question, answer) -> 
                                                { QuestionId = question.Id
                                                  EventId = eventId
                                                  Email = email
                                                  Answer = answer
                                                })
                                        Queries.createParticipantAnswers participantAnswerDbModels transaction
                                transaction.Commit()
                                Ok (participant, answers)
                            with
                            | ex ->
                                transaction.Rollback()
                                Error ex.Message
                                
                        connection.Close()
                        
                        let validatedInsertResult =
                            match insertResult with
                            | Ok (participant, answers) ->
                                match participant, answers with
                                    | Ok participant, Ok answers ->
                                        let answers = List.map (fun (answer: ParticipantAnswerDbModel) -> answer.Answer) answers
                                        Ok (participant, answers)
                                    | Error e1, Error e2 ->
                                       $"""Feil med lagring av deltaker og svar.
                                       Deltaker: {e1}.
                                       Spørsmål: {e2}."""
                                       |> Error
                                    | Error e, Ok _ ->
                                        Error $"Feil med lagring av deltaker: {e}"
                                    | Ok _, Error e ->
                                        Error $"Feil med lagring av deltakersvar: {e}"
                            | Error e -> Error e
                            
                        Result.map (fun (participant: DbModel, answers) ->
                            // FIXME: we need these domain models as the rest of the system all work with these
                            // Lage domenemodell av participant
                            let participantDomainModel = Participant.CreateFromPrimitives participant.Name participant.Email answers participant.EventId participant.RegistrationTime participant.CancellationToken participant.EmployeeId
                            // Lag domenemodell av event
                            let eventDomainModel = Event.Models.dbToDomain(event, [])
                            // Sende epost
                            let isWaitlisted = participate = IsWaitListed
                            let email =
                                let redirectUrlTemplate =
                                    HttpUtility.UrlDecode writeModel.CancelUrlTemplate

                                let createCancelUrl (participant: Participant) =
                                    redirectUrlTemplate.Replace("{eventId}",
                                                                participant.EventId.Unwrap.ToString
                                                                    ())
                                                       .Replace("{email}",
                                                                participant.Email.Unwrap
                                                                |> Uri.EscapeDataString)
                                                       .Replace("{cancellationToken}",
                                                                participant.CancellationToken.ToString
                                                                    ())
                                
                                Event.Service.createNewParticipantMail
                                    createCancelUrl eventDomainModel isWaitlisted
                                    (Email.Types.EmailAddress config.noReplyEmail)
                                    participantDomainModel
                            Email.Service.sendMail email context
                            
                            participantDomainModel
                            ) validatedInsertResult
                | Error e, _ -> Error e
                | _, Error e -> Error $"Fikk ikke til å parse request body: {e}"
                
            return!
                match result with
                | Ok result ->
                    let newlyCreatedParticipantViewModel = domainToViewWithCancelInfo result
                    json newlyCreatedParticipantViewModel next context
                | Error e ->
                    context.SetStatusCode 400
                    convertUserMessagesToHttpError [BadInput e] next context
        }
        
let getEventsForForsideHandler (email: string) =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let config = context.GetService<AppConfig>()
            
            use connection = new SqlConnection(config.databaseConnectionString)
            connection.Open()
            use transaction = connection.BeginTransaction()
            let! events = Queries.getEventsForForside email transaction
            transaction.Commit()
            connection.Close()
            
            match events with
            | Ok events ->  
                return! json events next context
            | Error e ->
                let message = convertUserMessagesToHttpError [NotFound e] next context
                return! message
        }
        
let routes: HttpHandler =
    choose
        [ POST
          >=> choose
                  [ routef "/events/%O/participants/%s" registerParticipationHandler ]
          GET
          >=> choose
                  [ isAuthenticatedV2 >=> routef "/events/forside/%s" getEventsForForsideHandler ]
        ]
