module Tests.TestUtils

open System
open System.Net.Mime
open System.Text
open System.Text.RegularExpressions
open Microsoft.AspNetCore.TestHost
open Microsoft.AspNetCore.Hosting
open System.Net.Http
open Thoth.Json.Net

[<Literal>]
let TokenEnvVariableName = "ARRANGEMENT_SVC_TEST_JWT_TOKEN"
let token = Environment.GetEnvironmentVariable TokenEnvVariableName

let basePath = "http://localhost:5000"

let lines (str : string) : string array =
    str.Split(System.Environment.NewLine)

let contains (needle: string) (haystack: string) =
        haystack.Contains(needle)

let matches (re: string) (str: string) =
    Regex(re).IsMatch(str)

let split (delim: string) (str : string) =
    str.Split(delim)

module Seq =
    let first (xs : 'a seq) : 'a =
        xs |> Seq.take 1 |> Seq.exactlyOne

    let tryFirst (xs : 'a seq) :'a option =
        let found = xs |> Seq.take 1
        if Seq.isEmpty found
        then None
        else Some (found |> Seq.exactlyOne)

module Option =
    let fromString (x: string) =
        if String.IsNullOrWhiteSpace(x)
        then None
        else Some x

// Giraffe stuff
let getTestHost() =
    WebHostBuilder()
        .UseTestServer()
        .Configure(App.configureApp)
        .ConfigureServices(App.configureServices)
        
let testRequest (request : HttpRequestMessage) =
    let resp = task {
        use server = new TestServer(getTestHost())
        use client = server.CreateClient()
        let! response = request |> client.SendAsync
        return response
    }
    let result = resp.Result
    async {
        do! Async.Sleep(1000)
    } |> Async.RunSynchronously
    result 
    
let request method jsonBody (url: string) token = 
    let request = new HttpRequestMessage()
    request.Method <- method
    request.RequestUri <- Uri(url)
    request.Content <- new StringContent(jsonBody, Encoding.UTF8, MediaTypeNames.Application.Json)
    request.Headers.Add("Authorization", $"Bearer {token}")
    
    let response = testRequest request
    response, response.Content.ReadAsStringAsync().Result
    
let getRequest (url: string) = request HttpMethod.Get "" url ""
let getRequestWithBody jsonBody (url: string) = request HttpMethod.Get jsonBody url ""
let getRequestAuthenticated (url: string) token = request HttpMethod.Get "" url token
let getRequestAuthenticatedWithBody jsonBody (url: string) token = request HttpMethod.Get jsonBody url token

let postRequest (url: string) = request HttpMethod.Post "" url ""
let postRequestWithBody jsonBody (url: string) = request HttpMethod.Post jsonBody url ""
let postRequestAuthenticated (url: string) token = request HttpMethod.Post "" url token
let postRequestAuthenticatedWithBody jsonBody (url: string) token = request HttpMethod.Post jsonBody url token

let putRequest (url: string) = request HttpMethod.Put "" url ""
let putRequestWithBody jsonBody (url: string) = request HttpMethod.Put jsonBody url ""
let putRequestAuthenticated (url: string) token = request HttpMethod.Put "" url token
let putRequestAuthenticatedWithBody jsonBody (url: string) token = request HttpMethod.Put jsonBody url token


let deleteRequest (url: string) = request HttpMethod.Delete "" url ""
let deleteRequestWithBody jsonBody (url: string) = request HttpMethod.Delete jsonBody url ""
let deleteRequestAuthenticated (url: string) token = request HttpMethod.Delete "" url token
let deleteRequestAuthenticatedWithBody jsonBody (url: string) token = request HttpMethod.Delete jsonBody url token

let toJson data = Encode.Auto.toString(4, data, caseStrategy = CamelCase)

let decodeForsideEvent content =
    Decode.Auto.fromString<{| id: string |}>(content)
    
let decodeAttendeesAndWaitlist content =
    Decode.Auto.fromString<{| attendees: {| email: string |} list; waitingList : {| email: string  |} list  |}>(content)
    |> function
    | Error _ -> failwith $"Error: {content}"
    | Ok created -> created
    
let decodeParticipant content =
    Decode.Auto.fromString<{| email: string |} list>(content)
    |> function
    | Error _ -> failwith $"Error: {content}"
    | Ok created -> created
    
let decodeParticipantWithCancellationToken content =
    Decode.Auto.fromString<{| cancellationToken: string |}>(content)
    |> function
    | Error _ -> failwith $"Error: {content}"
    | Ok created -> created
    
let decodeEvent content =
    Decode.Auto.fromString<{| isCancelled: bool |}>(content)
    |> function
        | Error _ -> failwith $"Error: {content}"
        | Ok created -> created
        
let decodeUserMessage content =
    Decode.Auto.fromString<{| userMessage: string |}>(content)
    |> function
        | Error _ -> failwith $"Error: {content}"
        | Ok created -> created
    
let postEvent event =
    let eventJson = event |> toJson
    let _, content = postRequestAuthenticatedWithBody eventJson "/events" token
    Decode.Auto.fromString<{| event: {| id: string; shortname: string option; isCancelled: bool |}; editToken: string |}> content
    |> function
        | Error _ -> failwith $"Error: {content}"
        | Ok created -> created