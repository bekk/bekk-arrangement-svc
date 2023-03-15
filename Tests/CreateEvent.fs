namespace Tests.CreateEvent

open Xunit
open System.Net

open Tests
open Models
open Http

[<Collection("Database collection")>]
type CreateEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    [<Fact>]
    member _.``Create event without authorization gives unauthorized``() =
        let event = Generator.generateEvent ()

        task {
            let! response, _ = Helpers.createEvent unauthenticatedClient event
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Create event with authorization works``() =
        let event = Generator.generateEvent ()

        task {
            let! response, _ = Helpers.createEvent authenticatedClient event
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Create event with office``() =
        let event =
            TestData.createEvent (fun e -> { e with Offices = [ Trondheim ] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event

            let! event = getEvent authenticatedClient createdEvent.Event.Id

            Assert.True(Result.isOk event)
            let actual =
                event
                |> Result.map (fun e -> e.Offices)
                |> Result.toOption
                |> Option.flatten
            Assert.Equal(Some Trondheim, actual)
        }
