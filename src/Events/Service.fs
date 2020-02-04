namespace ArrangementService.Event

open ArrangementService
open ArrangementService.Email
open CalendarInvite
open ResultComputationExpression
open Queries
open UserMessages
open ArrangementService.DomainModels
open Microsoft.AspNetCore.Http
open Giraffe

module Service =

    let models = ArrangementService.Event.Models.models
    let repo = Repo.from models

    let getEvents =
        result {
            for events in repo.read do
                return Seq.map models.dbToDomain events
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            for events in repo.read do

                let eventsByOrganizer =
                    queryEventsOrganizedBy organizerEmail events
                return Seq.map models.dbToDomain eventsByOrganizer
        }

    let getEvent id =
        result {
            for events in repo.read do

                let! event = events |> queryEventBy id
                return models.dbToDomain event
        }

    let private createEmail (event: Event) (context: HttpContext) =
        let config = context.GetService<AppConfig>()
        { Subject = sprintf "Du opprettet %s" event.Title.Unwrap
          Message = "Hei.."
          From = EmailAddress config.noReplyEmail
          To = event.OrganizerEmail
          CalendarInvite = createCalendarAttachment event event.OrganizerEmail }

    let sendNewlyCreatedEventMail (event: Event) =
        result {
            for mail in createEmail event >> Ok do
                yield Service.sendMail mail
        }

    let createEvent event =
        result {
            for newEvent in repo.create event do
                yield sendNewlyCreatedEventMail newEvent
                return newEvent
        }

    let updateEvent id event =
        result {
            for events in repo.read do
                let! oldEvent = events |> queryEventBy id
                return repo.update event oldEvent
        }

    let deleteEvent id =
        result {
            for events in repo.read do

                let! event = events |> queryEventBy id
                repo.del event
                return eventSuccessfullyDeleted id
        }
