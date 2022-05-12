module Participant.Authorization

open Giraffe

open Auth
open Http
open UserMessage
open Event.Models
open Participant.Models
open ResultComputationExpression

let userHasCancellationToken (eventId, email) =
    result {
        let! cancellationToken = queryParam "cancellationToken"

        let! participant = Event.Service.getParticipant
                               (Event.Types.Id eventId,
                                Email.Types.EmailAddress email)

        let hasCorrectCancellationToken =
            cancellationToken =
                participant.CancellationToken.ToString()

        if hasCorrectCancellationToken then
            return ()
        else
            return! [ AccessDenied "You cannot delete your participation without your cancellation token" ] |> Error |> Task.wrap
    }

let userCanCancel eventIdAndEmail =
    anyOf
        [ userHasCancellationToken eventIdAndEmail
          isAdmin ]

let eventHasAvailableSpots (event: Event) =
    result {
        let hasWaitingList = event.HasWaitingList
        let maxParticipants = event.MaxParticipants.Unwrap
        let! numberOfParticipants = Event.Service.getNumberOfParticipantsForEvent event.Id
        
        if hasWaitingList || maxParticipants.IsNone || numberOfParticipants.Unwrap < maxParticipants.Value
        then
            return ()
        else
            return! [ AccessDenied "Arrangementet er fullt" ] |> Error |> Task.wrap
    }

let eventIsNotCancelled (event: Event) =  
    result {
        if event.IsCancelled then
            return!
                Error [AccessDenied "Arrangementet er avlyst"]
                |> Task.wrap
        else
            return ()
    }

let oneCanParticipateOnEvent eventIdKey =
    result {
        let eventId = Event.Types.Id eventIdKey
        let! event = Event.Service.getEvent eventId
        do! eventHasAvailableSpots event
        do! Event.Authorization.eventHasNotPassed event 
        do! Event.Authorization.eventIsOpenForRegistration event 
        do! eventIsNotCancelled event 
        do! Event.Authorization.eventIsExternalOrUserIsAuthenticatedEvent event 
    }
