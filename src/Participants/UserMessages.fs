namespace ArrangementService.Participant

open ArrangementService
open UserMessage

module UserMessages =
    let participantNotFound email: UserMessage = 
        $"Kan ikke finne deltaker {email}" |> NotFound
    let participationNotFound (eventId, email): UserMessage =
        $"Kan ikke finne deltaker {email} for arrangement {eventId}" |> NotFound
    let participationSuccessfullyDeleted (eventId, email): string =
        $"Deltakelse for {email} på arrangement {eventId} ble slettet"
    let getParticipantsCountFailed (eventId): UserMessage =
        $"Henting av antall deltakere for {eventId} feilet" |> InternalErrorMessage
