namespace Tests.General

open System.Net
open System.Net.Http
open Tests
open Xunit

[<Collection("Database collection")>]
type General(fixture: DatabaseFixture) =
    let client = fixture.factory.CreateClient()
    let token = Some fixture.token

    [<Fact>]
    member _.``Health check without JWT token works`` () =
        task {
            let! response, _ = Http.get client None "/health"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Health check with JWT token works`` () =
        task {
            let! response, _ = Http.get client token "/health"
            response.EnsureSuccessStatusCode() |> ignore
        }

[<Collection("Database collection")>]
type CreateEventGeneral(fixture: DatabaseFixture) =
    let client = fixture.factory.CreateClient()
    let token = Some fixture.token

    [<Fact>]
    member _.``Create event without token fails`` () =
        let event = Generator.generateEvent()
        task {
            let! response, _ = Http.postEvent client None event
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Create event with token works`` () =
        let event = Generator.generateEvent()
        task {
            let! response, _ = Http.postEvent client token event
            response.EnsureSuccessStatusCode() |> ignore
        }
