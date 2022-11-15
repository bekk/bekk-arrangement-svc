module Models

open Models
open Thoth.Json.Net

type InnerEvent = { id: string; title: string }

type CreatedEvent =
    { editToken: string
      event: InnerEvent }

type InnerParticipant =
    { name: string
      email: string
      employeeId: int option
      participantAnswers: string list }

type CreatedParticipant =
    { participant: InnerParticipant
      cancellationToken: string }

type ParticipantAndAnswers = { name: string }

let participantAndAnswerDecoder: Decoder<ParticipantAndAnswers> =
    Decode.object (fun get -> { name = get.Required.Field "name" Decode.string })

type ParticipationsAndWaitlist =
    { attendees: ParticipantAndAnswers list
      waitingList: ParticipantAndAnswers list }

let participationsAndWaitingListDecoder: Decoder<ParticipationsAndWaitlist> =
    Decode.object (fun get ->
        { attendees = get.Required.Field "attendees" (Decode.list participantAndAnswerDecoder)
          waitingList = get.Required.Field "waitingList" (Decode.list participantAndAnswerDecoder) })

let innerEventDecoder: Decoder<InnerEvent> =
    Decode.object (fun get ->
        { id = get.Required.Field "id" Decode.string
          title = get.Required.Field "title" Decode.string })

let createdEventDecoder: Decoder<CreatedEvent> =
    Decode.object (fun get ->
        { editToken = get.Required.Field "editToken" Decode.string
          event = get.Required.Field "event" innerEventDecoder })

let innerParticipantDecoder: Decoder<InnerParticipant> =
    Decode.object (fun get ->
        { name = get.Required.Field "name" Decode.string
          email = get.Required.Field "email" Decode.string
          employeeId = get.Optional.Field "employeeId" Decode.int
          participantAnswers = get.Required.Field "participantAnswers" (Decode.list Decode.string) })

let createdParticipantDecoder: Decoder<CreatedParticipant> =
    Decode.object (fun get ->
        { cancellationToken = get.Required.Field "cancellationToken" Decode.string
          participant = get.Required.Field "participant" innerParticipantDecoder })

type UserMessage = { userMessage: string }

let decodeUserMessage content =
    if content = "" then
        { userMessage = "" }
    else
        match Decode.Auto.fromString<UserMessage> content with
        | Error e -> failwith $"Unable to decode usermessage: {e}"
        | Ok userMessage -> userMessage

type ParticipantTest =
    { WriteModel: ParticipantWriteModel
      Email: string
      CreatedModel: CreatedParticipant }

type ResponseBody =
    | Event of CreatedEvent
    | Participant of ParticipantTest
    | UserMessage of UserMessage

let getEvent (responseBody: ResponseBody): CreatedEvent =
    match responseBody with
    | Event createdEvent -> createdEvent
    | _ -> failwith "Not a valid created event model"

let useEvent (responseBody: ResponseBody) (f: CreatedEvent -> unit) =
    responseBody
    |> getEvent
    |> f

let getParticipant (responseBody: ResponseBody): ParticipantTest =
    match responseBody with
    | Participant participantTest -> participantTest
    | _ -> failwith "Not a valid participant test model"

let useParticipant (responseBody: ResponseBody) (f: ParticipantTest -> unit) =
    responseBody
    |> getParticipant
    |> f

let getUserMessage (responseBody: ResponseBody): UserMessage =
    match responseBody with
    | UserMessage userMessage -> userMessage
    | _ -> failwith "Not a valid userMessage"

let useUserMessage (responseBody: ResponseBody) (f: UserMessage -> unit): unit =
    responseBody
    |> getUserMessage
    |> f
