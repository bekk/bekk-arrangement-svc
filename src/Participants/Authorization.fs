namespace ArrangementService.Participant

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open ArrangementService.DomainModels
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

    
    let eventHasAvailableSpotsPure (event:Event) (numberOfParticipants:Event.NumberOfParticipants) = 
        let hasWaitingList = event.HasWaitingList
        let maxParticipants = event.MaxParticipants.Unwrap

        hasWaitingList  
        || (maxParticipants = 0)  // Infinite participants
        || numberOfParticipants.Unwrap < maxParticipants // Number of attendees less than the max

    let eventHasAvailableSpots eventId =
        result {
            //IO
            let! event = Event.Service.getEvent (Event.Id eventId)
            let! numberOfParticipants = Service.getNumberOfParticipantsForEvent event.Id
            
            //Pure
            if eventHasAvailableSpotsPure event numberOfParticipants
            then
                return ()
            else
                return! [ AccessDenied "The event is full" ] |> Error
        }
