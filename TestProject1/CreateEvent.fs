namespace Tests.CreateEvent

open Xunit
open System.Net

open Models
open Tests

module TestData =
    let createEvent (f: EventWriteModel -> EventWriteModel): EventWriteModel = Generator.generateEvent() |> f

// TODO: Disse helperne kan endres litt
module Helpers =
    let createEventTest client event =
        task {
            let! response, createdEvent = Http.postEvent client event

            match createdEvent with
            | Error e -> return failwith $"Unable to decode created event: {e}"
            | Ok createdEvent ->
                Assert.IsType<CreatedEvent>(createdEvent) |> ignore
                return response, createdEvent
        }

[<Collection("Database collection")>]
type CreateEvent(fixture: DatabaseFixture) =
    let authenticatedClient = fixture.getAuthedClient()
    let unauthenticatedClient = fixture.getUnauthenticatedClient()

    [<Fact>]
    member _.``Create event without authorization gives unauthorized`` () =
        let event = Generator.generateEvent()
        task {
            let! response, _ = Http.postEvent unauthenticatedClient event
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Create event with authorization works`` () =
        let event = Generator.generateEvent()
        task {
            let! response, _ = Helpers.createEventTest authenticatedClient event
            response.EnsureSuccessStatusCode() |> ignore
        }
