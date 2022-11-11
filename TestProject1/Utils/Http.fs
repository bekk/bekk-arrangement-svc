module Http

open System
open System.Net.Http
open System.Net.Mime
open System.Text
open Models
open Thoth.Json.Net

let private toJson data = Encode.Auto.toString(4, data, caseStrategy = CamelCase)

let decode<'a> (content : string) =
    content
    |> Decode.Auto.fromString<'a>

let request (client: HttpClient) (url: string) (body: 'a option) (method: HttpMethod) =
    task {
        let request = new HttpRequestMessage()
        request.Method <- method
        request.RequestUri <- Uri(url)
        body |> Option.iter (fun body -> request.Content <- new StringContent(toJson body, Encoding.UTF8, MediaTypeNames.Application.Json))

        let! response = request |> client.SendAsync
        let! content = response.Content.ReadAsStringAsync()
        return response, content
    }

let get (client: HttpClient) (url: string) =
    request client url None HttpMethod.Get

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
