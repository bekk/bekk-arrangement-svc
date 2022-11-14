namespace Tests.RegisterToEvent

open Xunit
open System.Net

open Tests

[<Collection("Database collection")>]
type RegisterToEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient ()

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient ()

    [<Fact>]
    member _.``Unauthenticated user can join external event``() =
        let generatedEvent = TestData.createEvent (fun e -> { e with IsExternal = true; MaxParticipants = Some 1 })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let! response, _ = Helpers.createParticipant unauthenticatedClient createdEvent.event.id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated user can not join internal event``() =
        let generatedEvent = TestData.createEvent (fun e -> { e with IsExternal = false; MaxParticipants = Some 1 })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let! response, _ = Helpers.createParticipant unauthenticatedClient createdEvent.event.id
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }
