module UserMessage

open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Net.Http.Headers

type UserMessage = { UserMessage: string }

type HttpStatus =
    | NotFound of string
    | BadRequest of string
    | Forbidden of string
    | InternalError of exn

let notFound message = RequestErrors.NOT_FOUND message
let badRequest message = RequestErrors.BAD_REQUEST message
let forbidden message = RequestErrors.FORBIDDEN message
let internalError exn = ServerErrors.INTERNAL_ERROR exn

let private httpStatusResult result handler next (context : HttpContext) =
    task {
        // TODO: How can we reference the module when a type shadows the name?
        let logger = context.GetService<ILogger<UserMessage>>()
        match! result with
        | Ok result ->
            return! handler result next context
        | Error (BadRequest e) ->
            logger.LogError("Bad Request: {message}", e)
            return! badRequest { UserMessage = e } next context
        | Error (NotFound e) ->
            logger.LogError("Not found: {message}", e)
            return! notFound { UserMessage = e } next context
        | Error (Forbidden e) ->
            logger.LogError("Forbidden: {message}", e)
            return! forbidden { UserMessage = e } next context
        | Error (InternalError ex) ->
            logger.LogError(ex, "Internal error")
            return! internalError { UserMessage = $"Det har skjedd en feil i backenden." } next context
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
    let participantNotFound email eventId: HttpStatus = $"Kan ikke finne deltaker med epost {email} på event {eventId}" |> NotFound
    let eventSuccessfullyCancelled title: string = $"Arrangement: '{title}' ble avlyst. Epost har blitt sendt til alle deltagere"
    let invalidMaxParticipantValue : HttpStatus = "Du kan ikke sette maks deltagere til lavere enn antall som allerede deltar" |> BadRequest
    let invalidRemovalOfWaitingList : HttpStatus = "Du kan ikke fjerne venteliste når det er folk på den" |> BadRequest
    let couldNotRetrieveUserId : HttpStatus = "Kunne ikke hente ut bruker-id" |> BadRequest
    let cannotSeeParticipations : HttpStatus = "Du har ikke tillatelse til å se andres påmeldinger" |> Forbidden
    let shortnameIsInUse shortname: HttpStatus = $"Det finnes allerede et pågående arrangement med kortnavn '{shortname}'" |> BadRequest
    let illegalQuestionsUpdate: HttpStatus = "Kan ikke endre på spørsmål som allerede har blitt stilt til deltakere" |> BadRequest
    let cannotUpdateEvent: HttpStatus = "Du har ikke rettigheter til å redigere dette arrangementet" |> Forbidden
    let cannotDeleteParticipation: HttpStatus = "Du kan ikke slette din deltagelse usen ditt cancellation token" |> Forbidden
    let mustBeAuthorizedOrEventMustBeExternal: HttpStatus = "Du må enten være innlogget eller arrangementet må være eksternt for at du skal få tilgang" |> Forbidden
