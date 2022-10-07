module Tests.Api

open System
open System.Net
open System.Net.Http
open System.Net.Mime
open System.Runtime.ExceptionServices
open System.Text

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Hosting
open Thoth.Json.Net
open Expecto

open Models
open TestUtils

[<Literal>]
let TokenEnvVariableName = "ARRANGEMENT_SVC_TEST_JWT_TOKEN"
let token = Environment.GetEnvironmentVariable TokenEnvVariableName

let basePath = "http://localhost:5000/api"

type TypedApiResponse<'a> = HttpResponseMessage*'a
type ApiResponse = TypedApiResponse<string>

let justEffect<'a> : TypedApiResponse<'a> Task -> Task = Task.ignore
let justResponse<'a> : TypedApiResponse<'a> Task -> HttpResponseMessage Task = Task.map fst
let justContent<'a> : TypedApiResponse<'a> Task -> 'a Task = Task.map snd

type ApiException (response: HttpResponseMessage, message: string) =
    inherit Exception(message)
    member _.Response = response

module Expect =
    open Expecto
    let throwsApiError<'a> (f: unit -> Task) (cont: ApiException -> 'a) : 'a Task =
       task {
           try
               do! f()
               failtest $"Expected exception of type %A{typeof<'a>}, but no exception was thrown."
               return (cont <| ApiException(null, null)) // <- to get the compiler to shut up
           with
           | :? ApiException as ex ->
               return (cont ex)
           | :? Expecto.AssertException as ex ->
               ExceptionDispatchInfo.Throw(ex) // reraise cannot be used in an async expression
               return (cont <| ApiException(null, null)) // <- to get the compiler to shut up
           | x ->
               failtest $"Expected exception of type ApiException to be thrown, but another exception was thrown, %A{x}"
               return (cont <| ApiException(null, null)) // <- to get the compiler to shut up
       }

    let expectApiError<'a> (f: unit -> TypedApiResponse<'a> Task) (expected : string) (message: string) : Task =
        throwsApiError
            (f >> Task.ignore)
            (fun ex -> Expect.equal ex.Message expected message)

    let expectApiErrorCode<'a> (expected: HttpStatusCode) (f: unit -> TypedApiResponse<'a> Task) (message: string) : Task =
        throwsApiError
            (f >> Task.ignore)
            (fun ex -> Expect.equal ex.Response.StatusCode expected message)

    let expectApiUnauthorized<'a> = expectApiErrorCode<'a> HttpStatusCode.Unauthorized

    let expectApiNotfound<'a> = expectApiErrorCode<'a> HttpStatusCode.NotFound

    let expectApiForbidden<'a> = expectApiErrorCode<'a> HttpStatusCode.Forbidden

    let expectApiSuccess (f: unit -> TypedApiResponse<_> Task) (message: string) : Task =
        task {
            let! (response, _) = f()
            Expect.isTrue response.IsSuccessStatusCode message
        }

    let expectApiMessage (f: unit -> ApiResponse Task) (expected: string) (message: string) : Task =
        task {
            let! (_, content) = f()
            Expect.equal content expected message
        }


let private apiError (response: HttpResponseMessage) (content: string) : 'a =
    if response.IsSuccessStatusCode then failwith $"Expected response to be a failure, but it is %A{response.StatusCode}"
    let msg =
        Decode.Auto.fromString<{| userMessage: string |}>(content)
        |> function
            | Ok x -> x.userMessage
            | Error _ -> content
    raise (ApiException(response, msg))

let private enforceSuccess ((response, content) : ApiResponse) : ApiResponse =
    if response.IsSuccessStatusCode
    then (response, content)
    else apiError response content

let private toJson data = Encode.Auto.toString(4, data, caseStrategy = CamelCase)

let webapp = new WebApplicationFactory<App.Program>()

let private client = webapp.CreateClient()

let private request (jwt: string option) (url: string) (body: 'a option) (method: HttpMethod) : ApiResponse Task =
    let request = new HttpRequestMessage()
    request.Method <- method
    request.RequestUri <- Uri(url)
    body |> Option.iter (fun body -> request.Content <- new StringContent(toJson body, Encoding.UTF8, MediaTypeNames.Application.Json))
    jwt |> Option.iter (fun jwt -> request.Headers.Add("Authorization", $"Bearer {jwt}"))
    task {
        let! response = request |> client.SendAsync
        let! content = response.Content.ReadAsStringAsync()
        return enforceSuccess (response, content)
    }
    //|> Task.map enforceSuccess

let private uriBuilder (url : string) : UriBuilder =
    UriBuilder(if url.StartsWith('/') then $"{basePath}{url}" else url)

[<RequireQualifiedAccess>]
module WithoutToken =
    let request (url: string) (body: 'a option) (method: HttpMethod) : ApiResponse Task =
        let url = (uriBuilder url).ToString()
        request None url body method

[<RequireQualifiedAccess>]
module UsingEditToken =
    let request (editToken: string) (url: string) (body: 'a option) (method: HttpMethod) : ApiResponse Task =
        let withToken =
            let builder = uriBuilder url
            builder.Query <- $"editToken={editToken}"
            builder.ToString()
        request None withToken body method

[<RequireQualifiedAccess>]
module UsingCancellationToken =
    let request (cancellationToken: string) (url: string) (body: 'a option) (method: HttpMethod) : ApiResponse Task =
        let withToken =
            let builder = uriBuilder url
            builder.Query <- $"cancellationToken={cancellationToken}"
            builder.ToString()
        request None withToken body method

[<RequireQualifiedAccess>]
module UsingShortName =
    let request (shortName: string) (url: string) (body: 'a option) (method: HttpMethod) : ApiResponse Task =
        let withToken =
            let builder = uriBuilder url
            builder.Query <- $"shortname={shortName}"
            builder.ToString()
        request None withToken body method

[<RequireQualifiedAccess>]
module UsingJwtToken =
    let request (url: string) (body: 'a option) (method: HttpMethod) : ApiResponse Task =
        let url = (uriBuilder url).ToString()
        request (Some token) url body method


[<AutoOpen>]
module Models =
    let mapResponse<'a, 'b> (f: 'a -> 'b) ((response, content) : TypedApiResponse<'a>) : TypedApiResponse<'b> =
        (response, f content)

    let decode<'a> (response : ApiResponse) : TypedApiResponse<'a> =
        response
        |> mapResponse (fun content ->
            Decode.Auto.fromString<'a>(content)
            |> function
                | Error err -> failwith $"Unable to deserialize content. Error: {err}, Content: {content}"
                | Ok x -> x
        )

    let decodeForsideEvent =
        decode<{| id: string |}>

    let decodeAttendeesAndWaitlist =
        decode<{| attendees: {| email: string |} list; waitingList : {| email: string  |} list  |}>

    let decodeParticipantEvents =
        decode<{| email: string |} list>

    type ParticipantResponse = {
        name: string
        email: string
        participantAnswers: string list
        registrationTime: string
        eventId: string
        cancellationToken: string
        employeeId: int option
    }

    type GetEventResponse = {
        id: string
        title: string
        description: string
        location: string
        organizerName: string
        organizerEmail: string
        // TODO: Fix deserialization of DateTimeCustom
        //startDate: DateTimeCustom.DateTimeCustom
        //endDate: DateTimeCustom.DateTimeCustom
        participantQuestions: string list
        // TODO: Fix deserialization. DateTime as ticks..?
        openForRegistrationTime: string
        hasWaitingList: bool
        isCancelled: bool
        isExternal: bool
        isHidden: bool
        organizerId: int
    }

    let decodeParticipiant =
        decode<ParticipantResponse>

    let decodeParticipantWithCancellationToken =
        decode<{| participant: ParticipantResponse; cancellationToken: string |}>

    let decodeEvent =
        decode<GetEventResponse>

    let decodeUserMessage =
        decode<{| userMessage: string |}>

    type CreatedEvent = {
        id: string
        shortName: string option
        isCancelled: bool
        editToken: string
        event: Models.EventWriteModel
    }

    type RegisteredParticipant = {
        participant: ParticipantWriteModel
        email: string
        event: CreatedEvent
        cancellationToken: string
        participantResponse: ParticipantResponse
    }


[<AutoOpen>]
module Api =
    type Requester<'a> = string -> 'a option -> HttpMethod -> ApiResponse Task
    let get req = req HttpMethod.Get
    let put req = req HttpMethod.Put
    let post req = req HttpMethod.Post
    let delete req = req HttpMethod.Delete

    module Health =
        let get (req: Requester<_>) =
            req "/health" None
            |> get
            |> Task.map decode<string>

    module Events =
        let create (req : Requester<Models.EventWriteModel>) (f : Models.EventWriteModel -> Models.EventWriteModel) : TypedApiResponse<CreatedEvent> Task =
            let event = f <| Generator.generateEvent()
            task {
                let! response = req "/events" (Some event) |> post
                return
                    response
                    |> decode<{| event: {| id: string; shortname: string option; isCancelled: bool |}; editToken: string |}>
                    |> mapResponse (fun created ->
                        { id = created.event.id
                          shortName = created.event.shortname
                          isCancelled = created.event.isCancelled
                          editToken = created.editToken
                          event = event })
            }

        let forside<'a> (req: Requester<'a>) (email: string) =
            req $"/events/forside/{email}" None
            |> get

        let future<'a> (req: Requester<'a>) =
            req $"/events" None
            |> get

        let previous<'a> (req: Requester<'a>) =
            req $"/events/previous" None
            |> get

        let get<'a> (req : Requester<'a>) (eventId : string) =
            req $"/events/{eventId}" None
            |> get
            |> Task.map decodeEvent

        let update (req : Requester<_>) (event: CreatedEvent) (f: EventWriteModel -> EventWriteModel) : TypedApiResponse<CreatedEvent> Task =
            let newEvent = f event.event
            req $"/events/{event.id}" (Some newEvent)
            |> put
            |> Task.map (mapResponse (fun _ -> { event with event = newEvent }))

        let cancel (req: Requester<_>) (event: CreatedEvent) : ApiResponse Task =
            req $"/events/{event.id}" None
            |> delete

        let delete (req: Requester<_>) (event: CreatedEvent) : ApiResponse Task =
            req $"/events/{event.id}/delete" None
            |> delete

    module Participant =
        let create (req: Requester<_>) (event: CreatedEvent) (fp: ParticipantWriteModel -> ParticipantWriteModel) (fe: string -> string) : TypedApiResponse<RegisteredParticipant> Task =
            let participant = fp (Generator.generateParticipant (List.length event.event.ParticipantQuestions))
            let email = fe (Generator.generateEmail())
            task {
                let! response = req $"/events/{event.id}/participants/{email}" (Some participant) |> post
                return
                    response
                    |> decodeParticipantWithCancellationToken
                    |> mapResponse (fun decoded ->
                        { participant = participant
                          email = email
                          event = event
                          participantResponse = decoded.participant
                          cancellationToken = decoded.cancellationToken })
            }

        let delete (req: Requester<_>) (participant: RegisteredParticipant) =
            req $"/events/{participant.event.id}/participants/{participant.email}" (Some participant)
            |> delete

module TestData =
    let createEvent (f: EventWriteModel -> EventWriteModel) : CreatedEvent Task =
        Events.create UsingJwtToken.request f
        |> justContent
