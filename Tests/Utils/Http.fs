module Http

open System
open System.Net.Http
open System.Net.Mime
open System.Text
open Models
open Thoth.Json.Net

let basePath = "http://localhost:5000/api"
let uriBuilder path = UriBuilder($"{basePath}/{path}")

let private extraCoders =
    Extra.empty
    |> Extra.withCustom DateTimeCustom.DateTimeCustom.encoder DateTimeCustom.DateTimeCustom.decoder
    |> Extra.withInt64

let private toJson data =
    Encode.Auto.toString (4, data, caseStrategy = CamelCase, extra = extraCoders)

let private autoDecoder<'T> : Decoder<'T> =
    Decode.Auto.generateDecoderCached<'T>(caseStrategy = CamelCase, extra = extraCoders)

let request (client: HttpClient) (url: string) (body: 'a option) (method: HttpMethod) =
    let url =
        if url.StartsWith("/") then
            Uri($"/api{url}", UriKind.Relative)
        else
            Uri(url)

    task {
        let request = new HttpRequestMessage()
        request.Method <- method
        request.RequestUri <- url

        body
        |> Option.iter (fun body ->
            request.Content <- new StringContent(toJson body, Encoding.UTF8, MediaTypeNames.Application.Json))

        let! response = request |> client.SendAsync
        let! content = response.Content.ReadAsStringAsync()
        return response, content
    }

let requestDecode<'a, 'b> client (decoder: Decoder<'a>) url (body: 'b option) method =
    task {
        let! response, content = request client url body method
        let decode =
            Decode.fromString decoder content
            |> Result.mapError(fun _ -> decodeUserMessage content)
        return response, decode
    }

let get (client: HttpClient) (url: string) = request client url None HttpMethod.Get

let getEvent (client: HttpClient) (eventId: string) =
    task {
        let! (_, content) = get client $"/events/{eventId}"
        let event : Result<Event, string> = Decode.fromString autoDecoder content
        return event
    }

let postParticipant (client: HttpClient) eventId email (participant: Models.ParticipantWriteModel) =
    requestDecode
        client
        createdParticipantDecoder
        $"/events/{eventId}/participants/{email}"
        (Some participant)
        HttpMethod.Post

let postEvent (client: HttpClient) (event: Models.EventWriteModel) =
    requestDecode client createdEventDecoder "/events" (Some event) HttpMethod.Post

let updateEvent (client: HttpClient) eventId (event: Models.EventWriteModel) =
    requestDecode client innerEventDecoder $"/events/{eventId}" (Some event) HttpMethod.Put

let deleteEvent (client: HttpClient) eventId =
    request client $"/events/{eventId}/delete" None HttpMethod.Delete

let deleteEventWithEditToken (client: HttpClient) eventId editToken =
    let url =
        let builder = uriBuilder $"events/{eventId}/delete"
        builder.Query <- $"editToken={editToken}"
        builder.ToString()
    request client url None HttpMethod.Delete

let cancelEvent (client: HttpClient) eventId =
    request client $"/events/{eventId}" None HttpMethod.Delete

let cancelEventWithEditToken (client: HttpClient) eventId editToken =
    let url =
        let builder = uriBuilder $"events/{eventId}"
        builder.Query <- $"editToken={editToken}"
        builder.ToString()
    request client url None HttpMethod.Delete

let updateEventWithEditToken (client: HttpClient) eventId editToken (event: Models.EventWriteModel) =
    let url =
        let builder = uriBuilder $"events/{eventId}"
        builder.Query <- $"editToken={editToken}"
        builder.ToString()

    requestDecode client innerEventDecoder url (Some event) HttpMethod.Put

let getCsvWithEditToken (client: HttpClient) eventId editToken =
    let url =
        let builder = uriBuilder $"events/{eventId}"
        builder.Query <- $"editToken={editToken}"
        builder.ToString()

    request client url None HttpMethod.Get


let getEventIdByShortname (client: HttpClient) (shortname: string) =
    let url =
        let builder = uriBuilder "events/id"
        builder.Query <- $"shortname={shortname}"
        builder.ToString()

    request client url None HttpMethod.Get

let getParticipationsForEvent (client: HttpClient) email =
    requestDecode
        client
        (participantAndAnswerDecoder |> Decode.list)
        $"/participants/{email}/events"
        None
        HttpMethod.Get

let getParticipationsAndWaitlist (client: HttpClient) eventId =
    requestDecode client participationsAndWaitingListDecoder $"/events/{eventId}/participants" None HttpMethod.Get

let deleteParticipantFromEvent (client: HttpClient) eventId email =
    request client $"/events/{eventId}/participants/{email}" None HttpMethod.Delete

let deleteParticipantFromEventWithCancellationToken (client: HttpClient) eventId email cancellationToken =
    let url =
        let builder = uriBuilder $"events/{eventId}/participants/{email}"
        builder.Query <- $"cancellationToken={cancellationToken}"
        builder.ToString()

    request client url None HttpMethod.Delete
