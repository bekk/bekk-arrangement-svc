namespace Tests

open System.Net
open Models
open Xunit

module TestData =
    let createEvent (f: EventWriteModel -> EventWriteModel) : EventWriteModel = Generator.generateEvent () |> f

module Helpers =
    let createEvent client event =
        task {
            let! response, createdEvent = Http.postEvent client event

            let responseBody =
                match createdEvent with
                | Ok createdEvent -> CreatedEvent createdEvent
                | Error userMessage -> UserMessage userMessage

            return response, responseBody
        }

    let createEventAndUse client event f =
        createEvent client event
        |> Task.map (fun (_, x) -> useCreatedEvent x f)

    let createEventAndGet client event =
        createEvent client event
        |> Task.map (fun (_, x) -> getCreatedEvent x)

    let updateEvent client eventId event =
        task {
            let! response, updatedEvent = Http.updateEvent client eventId event

            let responseBody =
                match updatedEvent with
                | Ok updatedEvent -> UpdatedEvent updatedEvent
                | Error userMessage -> UserMessage userMessage

            return response, responseBody
        }

    let updateEventAndUse client eventId event f =
        updateEvent client eventId event
        |> Task.map (fun (_, x) -> useUpdatedEvent x f)

    let rec updateEventAndGet client eventId event =
        updateEvent client eventId event
        |> Task.map (fun (_, x) -> getUpdatedEvent x)


    let updateEventWithEditTokenTest client eventId editToken event =
        task {
            let! response, updatedEvent = Http.updateEventWithEditToken client eventId editToken event

            match updatedEvent with
            | Error e -> return failwith $"Unable to decode updated event: {e}"
            | Ok updatedEvent ->
                Assert.IsType<InnerEvent>(updatedEvent) |> ignore
                return response, updatedEvent
        }

    let createParticipantForEvent client eventId email participant =
        task {
            let! response, createdParticipant = Http.postParticipant client eventId email participant

            let responseBody =
                match createdParticipant with
                | Ok createdParticipant ->
                    { WriteModel = participant
                      Email = email
                      CreatedModel = createdParticipant }
                    |> Participant
                | Error userMessage -> UserMessage userMessage

            return response, responseBody
        }

    let createParticipantwithQuestions client eventId numberOfQuestions =
        let participant =
            Generator.generateParticipant numberOfQuestions

        let email = Generator.generateEmail ()
        createParticipantForEvent client eventId email participant

    let createParticipant client eventId = createParticipantwithQuestions client eventId 0

    let createParticipantAndUse client eventId f =
        createParticipant client eventId
        |> Task.map (fun (_, x) -> useParticipant x f)

    let createParticipantAndGet client eventId =
        createParticipant client eventId
        |> Task.map (fun (_, x) -> getParticipant x)

    let getParticipationsAndWaitlist client eventId =
        task {
            let! response, result = Http.getParticipationsAndWaitlist client eventId

            match result with
            | Error e -> return failwith $"Failed to decode participations and waitlist: {e}"
            | Ok result -> return response, result
        }

    let getParticipationsForEvent client email =
        task {
            let! response, content = Http.getParticipationsForEvent client email

            match content with
            | Error e -> return failwith $"Error decoding participations and answers: {e}"
            | Ok result -> return response, result
        }

    let getPublicEvents client =
        task {
            let! response, content = Http.getPublicEvents client

            match content with
            | Error e -> return failwith $"Error decoding public events: {e}"
            | Ok result -> return response, result
        }
