namespace ArrangementService.Participant

open Giraffe

open ArrangementService
open Http
open ResultComputationExpression
open Models
open ArrangementService.Email
open Authorization
open System.Web
open System
open ArrangementService.DomainModels
open ArrangementService.Config
open ArrangementService.Event.Authorization

module Handlers =

    let registerForEvent (eventId: Guid, email) =
        result {
            let! writeModel = getBody<WriteModel>
            let redirectUrlTemplate =
                HttpUtility.UrlDecode writeModel.cancelUrlTemplate

            let createCancelUrl (participant: Participant) =
                redirectUrlTemplate.Replace("{eventId}",
                                            participant.EventId.Unwrap.ToString
                                                ())
                                   .Replace("{email}",
                                            participant.Email.Unwrap)
                                   .Replace("{cancellationToken}",
                                            participant.CancellationToken.ToString
                                                ())

            let! event = Event.Service.getEvent (Event.Id eventId)
            let! participants = Service.getParticipantsForEvent event
            let isWaitlisted =
                event.HasWaitingList
                && participants.attendees
                   |> Seq.length
                   >= event.MaxParticipants.Unwrap
            let! config = getConfig >> Ok
            let createMailForParticipant =
                Service.createNewParticipantMail
                    createCancelUrl event isWaitlisted
                    (EmailAddress config.noReplyEmail)

            let! participantDomainModel = (writeToDomain (eventId, email) writeModel) |> ignoreContext
            do! Service.registerParticipant createMailForParticipant participantDomainModel
            return domainToViewWithCancelInfo participantDomainModel
        }

    let getParticipationsForParticipant email =
        result {
            let! emailAddress = EmailAddress.Parse email |> ignoreContext
            let! participants = Service.getParticipationsForParticipant emailAddress
            return Seq.map domainToView participants |> Seq.toList
        }

    let deleteParticipant (id, email) =
        result {
            let! emailAddress = EmailAddress.Parse email |> ignoreContext
            
            let! event = Event.Service.getEvent (Event.Id id)
            let! deleteResult = Service.deleteParticipant (event, emailAddress) 
            
            return deleteResult
        }

    let getParticipantsForEvent id =
        result {
            let! event = Event.Service.getEvent (Event.Id id)
            let! participants = Service.getParticipantsForEvent event
            let hasWaitingList = event.HasWaitingList
            return { attendees =
                         Seq.map domainToView participants.attendees
                         |> Seq.toList
                     waitingList =
                         if hasWaitingList then
                             Seq.map domainToView
                                 participants.waitingList
                             |> Seq.toList
                             |> Some
                         else
                             None }
        }
    
    let getNumberOfParticipantsByEvent id =
        result {
            let! count = Service.getNumberOfParticipants (Event.Id id)
            return count.Unwrap
        }

    let routes: HttpHandler =
        choose
            [ GET
              >=> choose
                      [ routef "/events/%O/participants" (fun eventId ->
                            check (userCanSeeParticipants eventId)
                            >=> (handle << getParticipantsForEvent) eventId)
                        routef "/events/%O/participants/count" (fun eventId ->
                            (handle << getNumberOfParticipantsByEvent) eventId)
                        routef "/participants/%s/events"
                            (handle << getParticipationsForParticipant) ]
              DELETE
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun parameters ->
                            check (userCanCancel parameters)
                            >=> (handle << deleteParticipant) parameters) ]
              POST
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun (eventId: Guid, email) ->
                            check (eventHasAvailableSpots eventId)
                            >=> check
                                    (Event.Authorization.eventHasNotPassed
                                        eventId)
                            >=> check
                                    (Event.Authorization.eventHasOpenedForRegistration
                                        eventId)
                            >=> (handle << registerForEvent) (eventId, email)) ] ]
