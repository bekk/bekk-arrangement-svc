namespace ArrangementService.Event



open ArrangementService
open ArrangementService.DomainModels
open ArrangementService.ResultComputationExpression

module Validation =
    let assertNumberOfParticipantsLessThanOrEqualMax (event:Event) =
        result {
            let! numberOfParticipants = Participant.Queries.getNumberOfParticipantsForEvent event.Id
            if numberOfParticipants <= event.MaxParticipants.Unwrap then return () 
            else return! Error [UserMessages.invalidMaxParticipantValue] 
        }