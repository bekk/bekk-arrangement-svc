module Models

open Models
open Thoth.Json.Net

type Event = {
    Id: string
    Title: string
    Description: string
    Location: string
    StartDate: DateTimeCustom.DateTimeCustom
    EndDate: DateTimeCustom.DateTimeCustom
    OpenForRegistrationTime: int64
    CloseRegistrationTime: int64 option
    OrganizerName: string
    OrganizerEmail: string
    MaxParticipants: int option
    ParticipantQuestions: string[]
    Program: string option
    HasWaitingList: bool
    IsCancelled: bool
    IsExternal: bool
    IsHidden: bool
    NumberOfParticipants: int
    Shortname: string option
    CustomHexColor: string option
    Office: Office option
}

type InnerEvent = { Id: string
                    Title: string
                    Location: string
                    OrganizerName: string
                    OrganizerEmail: string
                    OrganizerId: int }

type CreatedEvent =
    { EditToken: string
      Event: InnerEvent }

type InnerParticipant =
    { Name: string
      Email: string
      EmployeeId: int option
      ParticipantAnswers: string list }

type CreatedParticipant =
    { Participant: InnerParticipant
      CancellationToken: string }

type ParticipantAndAnswers = { Name: string }

let participantAndAnswerDecoder: Decoder<ParticipantAndAnswers> =
    Decode.object (fun get -> { Name = get.Required.Field "name" Decode.string })

type ParticipationsAndWaitlist =
    { Attendees: ParticipantAndAnswers list
      WaitingList: ParticipantAndAnswers list }

let participationsAndWaitingListDecoder: Decoder<ParticipationsAndWaitlist> =
    Decode.object (fun get ->
        { Attendees = get.Required.Field "attendees" (Decode.list participantAndAnswerDecoder)
          WaitingList = get.Required.Field "waitingList" (Decode.list participantAndAnswerDecoder) })

let innerEventDecoder: Decoder<InnerEvent> =
    Decode.object (fun get ->
        { Id = get.Required.Field "id" Decode.string
          Title = get.Required.Field "title" Decode.string
          Location = get.Required.Field "location" Decode.string
          OrganizerEmail = get.Required.Field "organizerEmail" Decode.string
          OrganizerName = get.Required.Field "organizerName" Decode.string
          OrganizerId = get.Required.Field "organizerId" Decode.int
          })

let createdEventDecoder: Decoder<CreatedEvent> =
    Decode.object (fun get ->
        { EditToken = get.Required.Field "editToken" Decode.string
          Event = get.Required.Field "event" innerEventDecoder })

let innerParticipantDecoder: Decoder<InnerParticipant> =
    Decode.object (fun get ->
        { Name = get.Required.Field "name" Decode.string
          Email = get.Required.Field "email" Decode.string
          EmployeeId = get.Optional.Field "employeeId" Decode.int
          ParticipantAnswers = get.Required.Field "participantAnswers" (Decode.list Decode.string) })

let createdParticipantDecoder: Decoder<CreatedParticipant> =
    Decode.object (fun get ->
        { CancellationToken = get.Required.Field "cancellationToken" Decode.string
          Participant = get.Required.Field "participant" innerParticipantDecoder })

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
    | CreatedEvent of CreatedEvent
    | UpdatedEvent of InnerEvent
    | Participant of ParticipantTest
    | UserMessage of UserMessage

let getCreatedEvent (responseBody: ResponseBody): CreatedEvent =
    match responseBody with
    | CreatedEvent createdEvent -> createdEvent
    | _ -> failwith "Not a valid created event model"

let useCreatedEvent (responseBody: ResponseBody) (f: CreatedEvent -> unit) =
    responseBody
    |> getCreatedEvent
    |> f

let getUpdatedEvent (responseBody: ResponseBody): InnerEvent =
    match responseBody with
    | UpdatedEvent updatedEvent -> updatedEvent
    | _ -> failwith "Not a valid created event model"

let useUpdatedEvent (responseBody: ResponseBody) (f: InnerEvent -> unit) =
    responseBody
    |> getUpdatedEvent
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
