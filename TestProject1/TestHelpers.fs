namespace Tests

open System.Net
open Models
open Xunit

module TestData =
    let createEvent (f: EventWriteModel -> EventWriteModel) : EventWriteModel = Generator.generateEvent () |> f

module Helpers =
    let createEventTest client event =
        task {
            let! response, createdEvent = Http.postEvent client event

            let responseBody =
                match createdEvent with
                | Ok createdEvent -> Event createdEvent
                | Error userMessage -> UserMessage userMessage

            return response, responseBody
        }

    let updateEventTest client eventId event =
        task {
            let! response, updatedEvent = Http.updateEvent client eventId event

            match updatedEvent with
            | Error e -> return failwith $"Unable to decode updated event: {e}"
            | Ok updatedEvent ->
                Assert.IsType<InnerEvent>(updatedEvent) |> ignore
                return response, updatedEvent
        }

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

    let createParticipant client eventId =
        let participant =
            Generator.generateParticipant 0

        let email = Generator.generateEmail ()
        createParticipantForEvent client eventId email participant


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
