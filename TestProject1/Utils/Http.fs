module Http

open System
open System.Net.Http
open System.Net.Mime
open System.Text
open Models
open Thoth.Json.Net

let basePath = "http://localhost:5000/api/"
let uriBuilder path =
    UriBuilder($"{basePath}{path}")

let private toJson data =
    Encode.Auto.toString (4, data, caseStrategy = CamelCase)

let decode<'a> (content: string) = content |> Decode.Auto.fromString<'a>

let request (client: HttpClient) (url: string) (body: 'a option) (method: HttpMethod) =
    task {
        let request = new HttpRequestMessage()
        request.Method <- method
        request.RequestUri <- Uri(url)

        body
        |> Option.iter (fun body ->
            request.Content <- new StringContent(toJson body, Encoding.UTF8, MediaTypeNames.Application.Json))

        let! response = request |> client.SendAsync
        let! content = response.Content.ReadAsStringAsync()
        return response, content
    }

let get (client: HttpClient) (url: string) = request client url None HttpMethod.Get

let postParticipant (client: HttpClient) eventId email (participant: Models.ParticipantWriteModel) =
    task {
        let! response, content =
            request client $"/api/events/{eventId}/participants/{email}" (Some participant) HttpMethod.Post

        return response, Decode.fromString createdParticipantDecoder content
    }

let postEvent (client: HttpClient) (event: Models.EventWriteModel) =
    task {
        let! response, content = request client "/api/events" (Some event) HttpMethod.Post
        return response, Decode.fromString createdEventDecoder content
    }

let updateEvent (client: HttpClient) eventId (event: Models.EventWriteModel) =
    task {
        let! response, content = request client $"/api/events/{eventId}" (Some event) HttpMethod.Put
        return response, Decode.fromString innerEventDecoder content
    }

let updateEventWithEditToken (client: HttpClient) eventId editToken (event: Models.EventWriteModel) =
    let url =
        let builder = uriBuilder $"events/{eventId}"
        builder.Query <- $"editToken={editToken}"
        builder.ToString()

    task {
        let! response, content = request client url (Some event) HttpMethod.Put
        return response, Decode.fromString innerEventDecoder content
    }

let getEventIdByShortname (client: HttpClient) (shortname: string) =
    let url =
        let builder = uriBuilder "events/id"
        builder.Query <- $"shortname={shortname}"
        builder.ToString()

    task {
        let! response, content = request client url None HttpMethod.Get
        return response, content
    }

let getParticipationsForEvent (client: HttpClient) email =
    task {
        let! response, content = request client $"/api/participants/{email}/events" None HttpMethod.Get
        return response, (Decode.fromString (participantAndAnswerDecoder |> Decode.list) content)
    }


let getParticipationsAndWaitlist (client: HttpClient) eventId =
    task {
        let! response, content = request client $"/api/events/{eventId}/participants" None HttpMethod.Get
        return response, Decode.fromString participationsAndWaitingListDecoder content
    }
