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
