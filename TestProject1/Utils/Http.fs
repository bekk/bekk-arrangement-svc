module Http

open System
open System.Net.Http
open System.Net.Mime
open System.Text
open Thoth.Json.Net

let private toJson data = Encode.Auto.toString(4, data, caseStrategy = CamelCase)

let decode<'a> (content : string) =
    content
    |> Decode.Auto.fromString<'a>

let request (client: HttpClient) (jwt: string option) (url: string) (body: 'a option) (method: HttpMethod) =
    task {
        let request = new HttpRequestMessage()
        request.Method <- method
        request.RequestUri <- Uri(url)
        body |> Option.iter (fun body -> request.Content <- new StringContent(toJson body, Encoding.UTF8, MediaTypeNames.Application.Json))
        jwt |> Option.iter (fun jwt -> request.Headers.Add("Authorization", $"Bearer {jwt}"))
        let! response = request |> client.SendAsync
        let! content = response.Content.ReadAsStringAsync()
        return response, content
    }

let get (client: HttpClient) (jwt: string option) (url: string) =
    request client jwt url None HttpMethod.Get

let postEvent (client: HttpClient) (jwt: string option) (event: Models.EventWriteModel) =
    task {
        let! response, content = request client jwt "/api/events" (Some event) HttpMethod.Post
        return response, decode<Models.CreatedEvent>(content)
    }
