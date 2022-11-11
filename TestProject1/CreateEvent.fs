namespace Tests.CreateEvent

open Xunit
open System.Net

open Tests

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
