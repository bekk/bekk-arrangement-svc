namespace ArrangementService.Event

open ArrangementService

open Http
open ResultComputationExpression
open Models
open ArrangementService.DomainModels
open ArrangementService.Config
open ArrangementService.Email
open Validation
open Authorization

open Giraffe
open System.Web

module Handlers =

    let getEvents: Handler<ViewModel list> =
        result {
            let! events = Service.getEvents
            return Seq.map domainToView events |> Seq.toList
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            let! events = Service.getEventsOrganizedBy (EmailAddress organizerEmail)
            return Seq.map domainToView events |> Seq.toList
        }

    let getEvent id =
        result {
            let! event = Service.getEvent (Id id)
            return domainToView event
        }

    let deleteEvent id =
        result {
            let! messageToParticipants = getBody<string>
            let! event = Service.getEvent (Id id)
            let! participants = Participant.Service.getParticipantsForEvent event
            
            let! config = getConfig >> Ok

            yield Participant.Service.sendCancellationMailToParticipants
                      messageToParticipants (EmailAddress config.noReplyEmail) participants.attendees event

            let! result = Service.deleteEvent (Id id)
            return result
        }

    let updateEvent id =
        result {
            let! writeModel = getBody<WriteModel>
            let! domainModel = writeToDomain id writeModel |> ignoreContext

            do! assertNumberOfParticipantsLessThanOrEqualMax domainModel
            
            let! updatedEvent = Service.updateEvent (Id id) domainModel
            return domainToView updatedEvent
        }

    let createEvent =
        result {
            let! writeModel = getBody<WriteModel>

            let redirectUrlTemplate =
                HttpUtility.UrlDecode writeModel.editUrlTemplate

            let createEditUrl (event: Event) =
                redirectUrlTemplate.Replace("{eventId}",
                                            event.Id.Unwrap.ToString())
                                   .Replace("{editToken}",
                                            event.EditToken.ToString())

            let! newEvent = Service.createEvent createEditUrl writeModel

            return domainToViewWithEditInfo newEvent
        }

    let routes: HttpHandler =
        choose
            [ GET
              >=> choose
                      [ route "/events" >=> handle getEvents
                        routef "/events/%O" (handle << getEvent)
                        routef "/events/organizer/%s"
                            (handle << getEventsOrganizedBy) ]
              DELETE
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            check (userCanEditEvent id)
                            >=> (handle << deleteEvent) id) ]
              PUT
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            check (userCanEditEvent id)
                            >=> (handle << updateEvent) id) ]
              POST >=> choose [ route "/events" >=> handle createEvent ] ]
