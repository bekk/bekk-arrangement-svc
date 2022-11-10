namespace Tests.General

open System.Net
open Tests
open Xunit

[<Collection("Database collection")>]
type General(fixture: DatabaseFixture) =
    let authenticatedClient = fixture.getAuthedClient()
    let unauthenticatedClient = fixture.getUnauthenticatedClient()

    [<Fact>]
    member _.``Health check without JWT token works`` () =
        task {
            let! response, _ = Http.get unauthenticatedClient "/health"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Health check with JWT token works`` () =
        task {
            let! response, _ = Http.get authenticatedClient "/health"
            response.EnsureSuccessStatusCode() |> ignore
        }

[<Collection("Database collection")>]
type CreateEventGeneral(fixture: DatabaseFixture) =
    let authenticatedClient = fixture.getAuthedClient()
    let unauthenticatedClient = fixture.getUnauthenticatedClient()

    [<Fact>]
    member _.``Create event without token should give unauthorized`` () =
        let event = Generator.generateEvent()
        task {
            let! response, _ = Http.postEvent unauthenticatedClient event
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Create event with token works`` () =
        let event = Generator.generateEvent()
        task {
            let! response, _ = Http.postEvent authenticatedClient event
            response.EnsureSuccessStatusCode() |> ignore
        }
