namespace Tests.UpdateEvent

open System.Net
open Xunit

open Models
open Tests


// TODO: Legg til tester på legge til, sletting og endring av spørsmål
// TODO: Se på alle de mulige tingene som kan feile når man oppdaterer et event
[<Collection("Database collection")>]
type UpdateEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    [<Fact>]
    member _.``Edit event without without authorization gives forbidden``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent

            let eventToUpdate =
                { generatedEvent with Title = "This is a new title!" }

            let! response, _ = Http.updateEvent unauthenticatedClient createdEvent.event.id eventToUpdate
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Edit event with authorization works``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent

            let eventToUpdate =
                { generatedEvent with Title = "This is a new title!" }

            let! response, updatedEvent =
                Helpers.updateEventTest authenticatedClient createdEvent.event.id eventToUpdate

            Assert.Equal("This is a new title!", updatedEvent.title)
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Edit event without authorization but with edit token works``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent

            let eventToUpdate =
                { generatedEvent with Title = "This is a new title!" }

            let! response, updatedEvent =
                Helpers.updateEventWithEditTokenTest
                    unauthenticatedClient
                    createdEvent.event.id
                    createdEvent.editToken
                    eventToUpdate

            Assert.Equal("This is a new title!", updatedEvent.title)
            response.EnsureSuccessStatusCode() |> ignore
        }
