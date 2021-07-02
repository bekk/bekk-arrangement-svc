namespace ArrangementService.Participant

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open Auth
open ResultComputationExpression
open UserMessage
open Http

module Authorization =

    let userHasCancellationToken (eventId, email) =
        result {
            let! cancellationToken = queryParam "cancellationToken"

            let! participant = Service.getParticipant
                                   (Event.Id eventId,
                                    Email.EmailAddress email)

            let hasCorrectCancellationToken =
                cancellationToken =
                    participant.CancellationToken.ToString()

            if hasCorrectCancellationToken then
                return ()
            else
                return! [ AccessDenied "You cannot delete your participation without your cancellation token" ] |> Error
        }

    let userCanCancel eventIdAndEmail =
        anyOf
            [ userHasCancellationToken eventIdAndEmail
              isAdmin ]

    let eventHasAvailableSpots (event:DomainModels.Event) =
        result {
            let hasWaitingList = event.HasWaitingList
            let maxParticipants = event.MaxParticipants.Unwrap
            let! participants = Service.getParticipantsForEvent event
            
            if hasWaitingList || maxParticipants = 0 || participants.attendees |> Seq.length < maxParticipants
            then
                return ()
            else
                return! [ AccessDenied "Arrangementet er fullt" ] |> Error
        }
    let eventIsNotCancelled (event:DomainModels.Event) =  
        if event.IsCancelled then
            Error [AccessDenied "Arrangementet er avlyst"]
        else
            Ok ()

    let oneCanParticipateOnEvent eventIdKey =
        let eventId = Event.Id eventIdKey
        result {
            let! event = Event.Service.getEvent eventId
            do! eventHasAvailableSpots event
            do! Event.Authorization.eventHasNotPassed event
            do! Event.Authorization.eventHasOpenedForRegistration event
            do! eventIsNotCancelled event |> ignoreContext
        }
