module UserMessages.Events

open UserMessage

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

let invalidRemovalOfWaitingList : UserMessage = 
    $"Du kan ikke fjerne venteliste når det er folk på den" |> BadInput

let couldNotRetrieveUserId : UserMessage = 
    $"Kunne ikke hente ut bruker-id" |> InternalErrorMessage

let shortnameIsInUse shortname: UserMessage = 
    $"Det finnes allerede et pågående arrangement med kortnavn '{shortname}'" |> BadInput

let illegalQuestionsUpdate: UserMessage = 
    $"Kan ikke endre på spørsmål som allerede har blitt stilt til deltakere" |> BadInput