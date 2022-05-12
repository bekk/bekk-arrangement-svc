module UserMessages.Participants

open UserMessage
let participantNotFound email: UserMessage = 
    $"Kan ikke finne deltaker {email}" |> NotFound

let participationNotFound (eventId, email): UserMessage =
    $"Kan ikke finne deltaker {email} for arrangement {eventId}" |> NotFound

let participationSuccessfullyDeleted (eventId, email): string =
    $"Deltakelse for {email} på arrangement {eventId} ble slettet"

let getParticipantsCountFailed (eventId): UserMessage =
    $"Henting av antall deltakere for {eventId} feilet" |> InternalErrorMessage

let participantDuplicate (email): UserMessage =
    $"Du er allerede påmeldt med eposten {email}" |> BadInput
