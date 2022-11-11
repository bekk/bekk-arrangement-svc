namespace Tests

open Models
open Xunit

module TestData =
    let createEvent (f: EventWriteModel -> EventWriteModel) : EventWriteModel = Generator.generateEvent () |> f

module Helpers =
    let createEventTest client event =
        task {
            let! response, createdEvent = Http.postEvent client event

            match createdEvent with
            | Error e -> return failwith $"Unable to decode created event: {e}"
            | Ok createdEvent ->
                Assert.IsType<CreatedEvent>(createdEvent)
                |> ignore

                return response, createdEvent
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

            match createdParticipant with
            | Error e -> return failwith $"Unable to decode created participant: {e}"
            | Ok createdParticipant ->
                Assert.IsType<CreatedParticipant>(createdParticipant)
                |> ignore

                return response, {| WriteModel = participant; Email = email; CreatedModel = createdParticipant |}
        }

    let createParticipant client eventId =
        let participant = Generator.generateParticipant 0
        let email = Generator.generateEmail ()
        createParticipantForEvent client eventId email participant
