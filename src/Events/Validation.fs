namespace ArrangementService.Event

open ArrangementService
open ArrangementService.DomainModels
open ArrangementService.ResultComputationExpression

module Validation =

    (*
        Checks capacity of event still makes sense
        For instance if the event is full it is not
        allowed to decrease number of spots
    *)
    let assertValidCapacityChange (oldEvent: Event) (newEvent: Event) =
        result {
            let oldMax = oldEvent.MaxParticipants.Unwrap
            let newMax = newEvent.MaxParticipants.Unwrap

            // Man kan alltid øke så lenge den forrige
            // ikke var uendelig
            // og man har venteliste
            if newMax >= oldMax && oldMax <> 0 && newEvent.HasWaitingList then
                return ()
            else

            // Den nye er uendelig, all good
            if newMax = 0 then
                return ()
            else

            // Det er plass til alle
            let! numberOfParticipants = Participant.Queries.getNumberOfParticipantsForEvent newEvent.Id
            if numberOfParticipants <= newMax then
                return () 
            else

            // Dette er ikke lov her nede fordi vi sjekker over at
            // det ikke er plass til alle
            // Derfor vil det være frekt å fjerne ventelista
            let isRemovingWaitingList = newEvent.HasWaitingList = false && oldEvent.HasWaitingList = true
            if isRemovingWaitingList then
                return! Error [ UserMessages.invalidRemovalOfWaitingList ]
            else
            
            return! Error [ UserMessages.invalidMaxParticipantValue ]
        }
