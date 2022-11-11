namespace Tests.UpdateEvent

open System.Net
open Models
open Tests

open Xunit

// TODO: Legg til tester på legge til, sletting og endring av spørsmål


module Helpers =
    let updateEventTest client eventId event =
        task {
            let! response, updatedEvent = Http.updateEvent client eventId event

            match updatedEvent with
            | Error e -> return failwith $"Unable to decode updated event: {e}"
            | Ok updatedEvent ->
                Assert.IsType<InnerEvent>(updatedEvent) |> ignore
                return response, updatedEvent
        }

[<Collection("Database collection")>]
type UpdateEvent(fixture: DatabaseFixture) =
    let authenticatedClient = fixture.getAuthedClient()
    let unauthenticatedClient = fixture.getUnauthenticatedClient()

    [<Fact>]
    member _.``Edit event without token should give unauthorized`` () =
        let generatedEvent = Generator.generateEvent()
        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient generatedEvent
            let eventToUpdate = { generatedEvent with Title = "This is a new title!"}
            let! response, _ = Http.updateEvent unauthenticatedClient createdEvent.event.id eventToUpdate
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Create event with token works`` () =
        let generatedEvent = Generator.generateEvent()
        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient generatedEvent
            let eventToUpdate = { generatedEvent with Title = "This is a new title!"}
            let! response, updatedEvent = Helpers.updateEventTest authenticatedClient createdEvent.event.id eventToUpdate
            Assert.Equal("This is a new title!", updatedEvent.title)
            response.EnsureSuccessStatusCode() |> ignore
        }
