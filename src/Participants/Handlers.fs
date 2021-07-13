namespace ArrangementService.Participant

open Giraffe

open Microsoft.Net.Http.Headers
open System.Web
open System

open ArrangementService
open Http
open ResultComputationExpression
open Models
open ArrangementService.Email
open Authorization
open ArrangementService.DomainModels
open ArrangementService.Config
open ArrangementService.Event.Authorization
open ArrangementService.Auth

module Handlers =

    let registerForEvent (eventId: Guid, email) =
        result {
            let! writeModel = parseBody<WriteModel>

            let redirectUrlTemplate =
                HttpUtility.UrlDecode writeModel.cancelUrlTemplate

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

            let! event = Service.getEvent (Event.Id eventId)
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
            
            let! userId = Auth.getUserId >> Ok // None for external participants 

            let! participantDomainModel = (writeToDomain (eventId, email) writeModel userId) |> ignoreContext
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
            
            let! event = Service.getEvent (Event.Id id)
            let! deleteResult = Service.deleteParticipant (event, emailAddress) 
            
            return deleteResult
        }

    let getParticipantsForEvent id =
        result {
            let! event = Service.getEvent (Event.Id id)
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
    
    let getNumberOfParticipantsForEvent id =
        result {
            let! count = Service.getNumberOfParticipantsForEvent (Event.Id id)
            return count.Unwrap
        }
    
    let getWaitinglistSpot (eventId, email) = Service.getWaitinglistSpot (Event.Id eventId) (EmailAddress email) 


    let exportParticipationsDataForEvent (eventId) ctx = 
        let strRepresentation = Service.exportParticipationsDataForEvent (Event.Id eventId) ctx
        ctx.SetHttpHeader (HeaderNames.ContentType, "text/csv")
        ctx.SetHttpHeader (HeaderNames.ContentDisposition, "attachment; filename=hei.csv")
        strRepresentation

    let routes: HttpHandler =
        choose
            [ GET
              >=> choose
                      [ routef "/events/%O/participants" (fun eventId ->
                            check (userCanSeeParticipants eventId)
                            >=> (handle << getParticipantsForEvent) eventId)
                        routef "/events/%O/participants/count" (fun eventId -> 
                            check (eventIsExternalOrUserIsAuthenticated eventId)
                            >=> (handle << getNumberOfParticipantsForEvent) eventId)
                        routef "/events/%O/participants/export" (fun eventId -> 
                            check (userCanEditEvent eventId)
                            >=> (csvhandle << exportParticipationsDataForEvent) eventId)
                        routef "/events/%O/participants/%s/waitinglist-spot" (fun (eventId, email) ->
                            check (eventIsExternalOrUserIsAuthenticated eventId)
                            >=> (handle << getWaitinglistSpot) (eventId, email))
                        routef "/participants/%s/events" (fun email ->
                            check isAuthenticated
                            >=> (handle << getParticipationsForParticipant) email) ]
              DELETE
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun parameters ->
                            check (userCanCancel parameters)
                            >=> (handle << deleteParticipant) parameters) ]
              POST
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun (eventId: Guid, email) ->
                            (check (oneCanParticipateOnEvent eventId)
                            >=> (handle << registerForEvent) (eventId, email))
                            |> withRetry) ] ]
