namespace ArrangementService.Event

open Giraffe

open ArrangementService
open ArrangementService.DomainModels
open Auth
open ResultComputationExpression
open UserMessage
open Http
open DateTime
open System

module Authorization =

    let userIsOrganizer (event: DomainModels.Event) =
        result {
            let! userId = getUserId

            let isTheOrganizer = 
                userId = Some event.OrganizerId.Unwrap

            if isTheOrganizer then
                return ()
            else
                return!
                    [ AccessDenied
                          $"Du prøver å endre på et arrangement (id {event.Id.Unwrap}) som du ikke er arrangør av" ]
                    |> Error
                    |> Task.wrap
        }

    let userHasCorrectEditToken (event: DomainModels.Event) =
        result {
            let! editToken = queryParam "editToken"

            let hasCorrectEditToken = editToken = event.EditToken.ToString()

            if hasCorrectEditToken then
                return ()
            else
                return!
                    [ AccessDenied
                          $"Du prøvde å gjøre endringer på et arrangement (id {event.Id.Unwrap}) med ugyldig editToken" ]
                    |> Error
                    |> Task.wrap
        }


    let userCanEditEvent eventId =
        result {
            let! event = Service.getEvent (Event.Id eventId)

            let! authResult =
                anyOf [ isAdmin
                        userHasCorrectEditToken event
                        userIsOrganizer event ]

            return authResult
        }

    let eventIsOpenForRegistration (event: DomainModels.Event) =
        result {
            let openDateTime =
                DateTimeOffset.FromUnixTimeMilliseconds event.OpenForRegistrationTime.Unwrap

            let closeDateTime = 
                Option.map DateTimeOffset.FromUnixTimeMilliseconds event.CloseRegistrationTime.Unwrap 

            let never = "aldri"

            if openDateTime <= DateTimeOffset.Now && (if closeDateTime.IsSome then DateTimeOffset.Now <= closeDateTime.Value else true) then
                return ()
            else
                return!
                    Error [ AccessDenied $"Arrangementet åpner for påmelding {openDateTime.ToLocalTime().ToIsoString()} og stenger {if closeDateTime.IsSome then closeDateTime.Value.ToLocalTime().ToIsoString() else never}" ]
                    |> Task.wrap
        }

    let eventHasNotPassed (event: DomainModels.Event) =
        result {
            if event.EndDate > now () then
                return ()
            else
                return!
                    Error [ AccessDenied "Arrangementet har allerede funnet sted" ]
                    |> Task.wrap
        }


    // TODO: UNDUPLICATE CODE:
    let eventIsExternal (eventId: Key) =
        result {
            let! event = Service.getEvent (Event.Id eventId)

            if event.IsExternal then
                return ()
            else
                return! Error [ AccessDenied "Arrangementet er internt" ] |> Task.wrap
        }

    let eventIsExternalOrUserIsAuthenticated (eventId: Key) =
        anyOf [ eventIsExternal eventId
                isAuthenticated ]


    let eventIsExternalEvent (event: Event) =
        result {
            if event.IsExternal then
                return ()
            else
                return! Error [ AccessDenied "Arrangementet er internt" ] |> Task.wrap
        }

    let eventIsExternalOrUserIsAuthenticatedEvent (event: Event) =
        anyOf [ eventIsExternalEvent event
                isAuthenticated ]
