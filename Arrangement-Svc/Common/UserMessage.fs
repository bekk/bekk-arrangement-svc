module UserMessage

open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Data.SqlClient
open Microsoft.Net.Http.Headers

type UserMessage = { UserMessage: string }

type HttpStatus =
    | NotFound of string
    | BadRequest of string
    | Forbidden of string
    | InternalError of exn

let internal_error_and_rollback_transaction (transaction: SqlTransaction) ex =
    transaction.Rollback()
    InternalError ex
let notFound message = RequestErrors.NOT_FOUND message
let badRequest message = RequestErrors.BAD_REQUEST message
let forbidden message = RequestErrors.FORBIDDEN message
let internalError exn = ServerErrors.INTERNAL_ERROR exn
let private httpStatusResult result handler next context =
    task {
        match! result with
        | Ok result -> return! handler result next context
        | Error (BadRequest e) -> return! badRequest { UserMessage = e } next context
        | Error (NotFound e) -> return! notFound { UserMessage = e } next context
        | Error (Forbidden e) -> return! forbidden { UserMessage = e } next context
        | Error (InternalError e) ->
            let logger = context.GetService<Bekk.Canonical.Logger.Logger>()
            logger.log(Bekk.Canonical.Logger.Error, "Exception", e.ToString())
            return! internalError { UserMessage = $"Det har skjedd en feil i backenden. Feil registrert med id: {logger.getRequestId()}. Kontakt basen dersom dette vedvarer." } next context
    }
let jsonResult result =
    fun next context ->
        task {
            return! httpStatusResult result json next context
        }
    
let csvResult filename result (next: HttpFunc) (context: HttpContext) =
    task {
        context.SetHttpHeader (HeaderNames.ContentType, "text/csv")
        context.SetHttpHeader (HeaderNames.ContentDisposition, $"attachment; filename=\"{filename}.csv\"")
        return! httpStatusResult result text next context
    }
    
module ResponseMessages =
    let eventNotFound id: HttpStatus = $"Kan ikke finne event {id}" |> NotFound
    let eventSuccessfullyCancelled title: string = $"Arrangement: '{title}' blei avlyst. Epost har blitt sendt til alle deltagere"
    let invalidMaxParticipantValue : HttpStatus = "Du kan ikke sette maks deltagere til lavere enn antall som allerede deltar" |> BadRequest
    let invalidRemovalOfWaitingList : HttpStatus = "Du kan ikke fjerne venteliste når det er folk på den" |> BadRequest
    let couldNotRetrieveUserId : HttpStatus = "Kunne ikke hente ut bruker-id" |> BadRequest
    let cannotSeeParticipations : HttpStatus = "Du har ikke tillatelse til å se andres påmeldinger" |> Forbidden
    let shortnameIsInUse shortname: HttpStatus = $"Det finnes allerede et pågående arrangement med kortnavn '{shortname}'" |> BadRequest
    let illegalQuestionsUpdate: HttpStatus = "Kan ikke endre på spørsmål som allerede har blitt stilt til deltakere" |> BadRequest
    let cannotUpdateEvent: HttpStatus = "Du har ikke rettigheter til å redigere dette arrangementet" |> Forbidden
    let cannotDeleteParticipation: HttpStatus = "Du kan ikke slette din deltagelse usen ditt cancellation token" |> Forbidden
