module Event.Validation

open Event.Models
open ResultComputationExpression

(*
    Checks capacity of event still makes sense
    For instance if the event is full it is not
    allowed to decrease number of spots
*)
let assertValidCapacityChange (oldEvent: Event) (newEvent: Event) =
    result {
        match oldEvent.MaxParticipants.Unwrap, newEvent.MaxParticipants.Unwrap with
        | _, None ->
            // Den nye er uendelig, all good
            return ()

        | None, Some newMax ->
            // Det er plass til alle
            let! numberOfParticipants = Participant.Queries.queryNumberOfParticipantsForEvent newEvent.Id
            if numberOfParticipants <= newMax then
                return () 
            else
            
            return! Error [ UserMessages.Events.invalidMaxParticipantValue ] |> Task.wrap

        | Some oldMax, Some newMax -> 
            // Man kan alltid øke så lenge den forrige
            // ikke var uendelig
            // og man har venteliste
            if newMax >= oldMax && newEvent.HasWaitingList then
                return ()
            else

            // Det er plass til alle
            let! numberOfParticipants = Participant.Queries.queryNumberOfParticipantsForEvent newEvent.Id
            if numberOfParticipants <= newMax then
                return () 
            else

            // Dette er ikke lov her nede fordi vi sjekker over at
            // det ikke er plass til alle
            // Derfor vil det være frekt å fjerne ventelista
            let isRemovingWaitingList = newEvent.HasWaitingList = false && oldEvent.HasWaitingList = true
            if isRemovingWaitingList then
                return! Error [ UserMessages.Events.invalidRemovalOfWaitingList ] |> Task.wrap
            else
            
            return! Error [ UserMessages.Events.invalidMaxParticipantValue ] |> Task.wrap
    }
