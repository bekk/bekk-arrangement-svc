namespace ArrangementService.Event

open ArrangementService
open UserMessage

module UserMessages =
    let eventNotFound id: UserMessage =
        $"Kan ikke finne event {id}" |> NotFound
    let cantUpdateEvent id: UserMessage =
        $"Kan ikke oppdatere event {id}" |> NotFound
    let eventSuccessfullyDeleted id: string =
        $"Event {id} blei sletta"
    let eventSuccessfullyCancelled title: string =
        $"Arrangement: '{title}' blei avlyst. Epost har blitt sendt til alle deltagere"
    let invalidMaxParticipantValue : UserMessage = 
        $"Du kan ikke sette maks deltagere til lavere enn antall som allerede deltar" |> BadInput
