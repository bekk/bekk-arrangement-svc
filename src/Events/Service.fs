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
        |> String.concat "<br>"

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


(* 
    This function fetches the editToken from the database so it fits with our
    writeToDomain function. Every field in the writeModel-records needs to be present when
    updating, even though you might only want to update one field. Its not a very
    intuitive solution but it works for now
    -- Summer intern 2021
*)
    let updateEvent (id:Event.Id) writeModel =
        result {
            let! editToken = Queries.queryEditTokenByEventId id
            let! newEvent = writeToDomain id.Unwrap writeModel editToken |> ignoreContext

            do! assertNumberOfParticipantsLessThanOrEqualMax newEvent
            do! Queries.updateEvent newEvent
            return newEvent 
        }

    let cancelEvent event =
        result {
            do! Queries.updateEvent {event with IsCancelled=true}
            return eventSuccessfullyCancelled event.Title
        }
    
    let deleteEvent id =
        result {
            do! Queries.deleteEvent id
            return eventSuccessfullyDeleted id
        }
