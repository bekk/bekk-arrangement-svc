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

    let userIsOrganizer eventId =
        result {
            let! event = Service.getEvent (Id eventId)

            let! userId = getUserId >> Ok


            let isTheOrganizer = userId = Some event.OrganizerId.Unwrap

            if isTheOrganizer then
                return ()
            else
                return!
                    [ AccessDenied $"You are trying to edit an event (id {eventId}) which you did not create" ]
                    |> Error
        }

    let userHasCorrectEditToken eventId =
        result {
            let! event = Service.getEvent (Id eventId)
            let! editToken = queryParam "editToken"

            let hasCorrectEditToken = editToken = event.EditToken.ToString()

            if hasCorrectEditToken then
                return ()
            else
                return!
                    [ AccessDenied $"You are trying to edit an event (id {eventId}) using the wrong editToken" ]
                    |> Error
        }


    let userCanEditEvent eventId =
        anyOf [ isAdmin
                userHasCorrectEditToken eventId
                userIsOrganizer eventId ]

    let userCanSeeParticipants = userCanEditEvent

    let eventHasOpenedForRegistration (event: DomainModels.Event) =
        let openDateTime =
            DateTimeOffset.FromUnixTimeMilliseconds event.OpenForRegistrationTime.Unwrap

        if openDateTime <= DateTimeOffset.Now then
            Ok()
        else
            Error [ AccessDenied $"Arrangementet åpner for påmelding {openDateTime.ToLocalTime}" ]

    let eventHasNotPassed (event: DomainModels.Event) =
        if event.EndDate > now () then
            Ok()
        else
            Error [ AccessDenied "Arrangementet har allerede funnet sted" ]


    let eventIsExternal (eventId: Key) =
        result {
            let! event = Service.getEvent (Event.Id eventId)

            if event.IsExternal then
                return ()
            else
                return! Error [ AccessDenied "Event is internal" ]
        }

    let eventIsExternalOrUserIsAuthenticated (eventId: Key) =
        anyOf [ eventIsExternal eventId
                isAuthenticated ]
