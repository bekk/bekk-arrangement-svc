module Models

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
    Decode.object (fun get ->
        { name = get.Required.Field "name" Decode.string })

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
