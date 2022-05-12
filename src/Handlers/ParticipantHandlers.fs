module Participant.Handlers

open System
open Giraffe
open System.Web
open System.Threading

open Http
open Auth
open Config
open Email.Types
open Participant.Models
open ResultComputationExpression

let registerForEvent (eventId: Guid, email) =
    result {
        // let! () = fun ctx -> Ok () |> Task.wrap
        // let timer = new Diagnostics.Stopwatch()
        // timer.Start()
        let! writeModel = parseBody<WriteModel>

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

        let! event = Event.Service.getEvent (Event.Types.Id eventId)
        let! participants = Event.Service.getNumberOfParticipantsForEvent event.Id
        let isWaitlisted =
            event.HasWaitingList
            && event.MaxParticipants.Unwrap.IsSome
            && participants.Unwrap
               >= event.MaxParticipants.Unwrap.Value
        let! config = getConfig >> Ok >> Task.wrap
        let createMailForParticipant =
            Event.Service.createNewParticipantMail
                createCancelUrl event isWaitlisted
                (EmailAddress config.noReplyEmail)
        
        let! userId = getUserId 

        let! participantDomainModel = writeToDomain (eventId, email) writeModel userId |> Task.wrap |> ignoreContext
        do! Event.Service.registerParticipant createMailForParticipant participantDomainModel
        
        return domainToViewWithCancelInfo participantDomainModel
    }

let getParticipationsForParticipant email =
    result {
        let! emailAddress = EmailAddress.Parse email |> Task.wrap |> ignoreContext
        let! participants = Event.Service.getParticipationsForParticipant emailAddress
        return Seq.map domainToView participants |> Seq.toList
    }

let deleteParticipant (id, email) =
    result {
        let! emailAddress = EmailAddress.Parse email |> Task.wrap |> ignoreContext
        
        let! event = Event.Service.getEvent (Event.Types.Id id)
        let! deleteResult = Event.Service.deleteParticipant (event, emailAddress) 
        
        return deleteResult
    }

let getParticipantsForEvent id =
    result {
        let! event = Event.Service.getEvent (Event.Types.Id id)
        let! participants = Event.Service.getParticipantsForEvent event
        let hasWaitingList = event.HasWaitingList
        let! userCanGetInformation = 
            Event.Authorization.userCanEditEvent id
                >> Task.map (function 
                | Ok () -> Ok true
                | Error _ -> Ok false)

        return { attendees =
                     Seq.map domainToView participants.attendees
                     |> Seq.map (fun p -> if not userCanGetInformation then {p with Email = None; ParticipantAnswers = []} else p) 
                     |> Seq.toList
                 waitingList =
                     if hasWaitingList then
                         Seq.map domainToView
                             participants.waitingList
                         |> Seq.map (fun p -> if not userCanGetInformation then {p with Email = None; ParticipantAnswers = []} else p) 
                         |> Seq.toList
                         |> Some
                     else
                         None }
    }

let getNumberOfParticipantsForEvent id =
    result {
        let! count = Event.Service.getNumberOfParticipantsForEvent (Event.Types.Id id)
        return count.Unwrap
    }

let getWaitinglistSpot (eventId, email) = Event.Service.getWaitinglistSpot (Event.Types.Id eventId) (EmailAddress email) 


let exportParticipationsDataForEvent eventId = Event.Service.exportParticipationsDataForEvent (Event.Types.Id eventId)

let registrationLock = new SemaphoreSlim(1,1)

let routes: HttpHandler =
    choose
        [ GET_HEAD
          >=> choose
                  [ routef "/events/%O/participants" (fun eventId ->
                        check isAuthenticated
                        >=> (handle << getParticipantsForEvent) eventId
                        |> withTransaction)
                    routef "/events/%O/participants/count" (fun eventId -> 
                        check (Event.Authorization.eventIsExternalOrUserIsAuthenticated eventId)
                        >=> (handle << getNumberOfParticipantsForEvent) eventId
                        |> withTransaction)
                    routef "/events/%O/participants/export" (fun eventId -> 
                        check (Event.Authorization.userCanEditEvent eventId)
                        >=> (csvHandle eventId << exportParticipationsDataForEvent) eventId
                        |> withTransaction)
                    routef "/events/%O/participants/%s/waitinglist-spot" (fun (eventId, email) ->
                        check (Event.Authorization.eventIsExternalOrUserIsAuthenticated eventId)
                        >=> (handle << getWaitinglistSpot) (eventId, email)
                        |> withTransaction)
                    routef "/participants/%s/events" (fun email ->
                        check isAuthenticated
                        >=> (handle << getParticipationsForParticipant) email
                        |> withTransaction) ]
          DELETE
          >=> choose
                  [ routef "/events/%O/participants/%s" (fun parameters ->
                        check (Authorization.userCanCancel parameters)
                        >=> (handle << deleteParticipant) parameters)
                        |> withTransaction ]

          POST
          >=> choose
                  [ routef "/events/%O/participants/%s" V2.Handlers.registerParticipationHandler ]
                   ]


