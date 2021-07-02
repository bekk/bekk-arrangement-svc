namespace ArrangementService.Event

open Giraffe

open ArrangementService
open Auth
open ResultComputationExpression
open UserMessage
open Http
open DateTime
open System

module Authorization =

    let userCreatedEvent eventId =
        result {
            let! editToken = queryParam "editToken"

            let! event = Service.getEvent (Id eventId)

            let hasCorrectEditToken =
                editToken = event.EditToken.ToString()

            if hasCorrectEditToken then
                return ()
            else
                return! [ AccessDenied $"You are trying to edit an event (id {eventId}) which you did not create"] |> Error
        }
    

    let userCanEditEvent eventId =
        anyOf
            [ userCreatedEvent eventId
              isAdmin ]

    let userCanSeeParticipants = userCanEditEvent

    let eventHasOpenedForRegistration (event:DomainModels.Event) =
        result {
            let openDateTime =
                DateTimeOffset.FromUnixTimeMilliseconds
                    event.OpenForRegistrationTime.Unwrap

            if openDateTime <= DateTimeOffset.Now then
                return ()
            else
                return! [ AccessDenied $"Arrangementet åpner for påmelding {openDateTime.ToLocalTime}" ] |> Error
        }

    let eventHasNotPassed (event:DomainModels.Event) =
        result {
            if event.EndDate > now() then
                return ()
            else
                return! [ AccessDenied "Arrangementet har allerede funnet sted" ] |> Error
        }
