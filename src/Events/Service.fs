namespace ArrangementService.Event

open ArrangementService
open ArrangementService.Email
open ResultComputationExpression
open UserMessages
open Validation
open ArrangementService.DomainModels
open Http
open Models

module Service =

    let getEvents: Handler<Event seq> =
        result {
            let! events = Queries.getEvents >> Ok
            return events
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            let! eventsByOrganizer = Queries.queryEventsOrganizedByEmail organizerEmail >> Ok
            return eventsByOrganizer
        }

    let getEvent id =
        result {
            let! event = Queries.queryEventByEventId id
            return event
        }

    let private createdEventMessage createEditUrl (event: Event) =
        [ "Hei! 😄"
          $"Du har nå opprettet {event.Title.Unwrap}."
          $"Her er en unik lenke for å endre arrangementet: {createEditUrl event}."
          "Ikke del denne med andre🕵️" ]
        |> String.concat "\n"

    let private createEmail createEditUrl (event: Event) =
        let message = createdEventMessage createEditUrl event
        { Subject = $"Du opprettet {event.Title.Unwrap}"
          Message = message
          To = event.OrganizerEmail
          CalendarInvite = None }

    let private sendNewlyCreatedEventMail createEditUrl (event: Event) =
        result {
            let mail =
                createEmail createEditUrl event
            yield Service.sendMail mail
        }

    let createEvent createEditUrl event =
        result {
            let! newEvent = Queries.createEvent event

            yield sendNewlyCreatedEventMail createEditUrl newEvent

            return newEvent
        }


    let updateEvent (id:Event.Id) writeModel =
        result {
            let! editToken = Queries.queryEditTokenByEventId id
            let! newEvent = writeToDomain id.Unwrap writeModel editToken |> ignoreContext

            do! assertNumberOfParticipantsLessThanOrEqualMax newEvent
            do! Queries.updateEvent newEvent
            return newEvent 
        }
    
    let deleteEvent id =
        result {
            do! Queries.deleteEvent id
            return eventSuccessfullyDeleted id
        }
