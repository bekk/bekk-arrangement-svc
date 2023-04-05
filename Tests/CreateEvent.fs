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
            TestData.createEvent (fun e -> { e with Offices = Some [ Trondheim ] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event

            let! event = getEvent authenticatedClient createdEvent.Event.Id

            Assert.True(Result.isOk event)
            let actual = event |> Result.map (fun e -> e.Offices)
            match actual with
            | Ok office ->
                Assert.True(office.IsSome)
                Assert.Single(office.Value) |> ignore
                Assert.Equal(List.head office.Value, Trondheim)
            | Error e -> failwith e
        }
    [<Fact>]
    member _.``Create event with two offices``() =
        let event =
            TestData.createEvent (fun e -> { e with Offices = Some [ Oslo; Trondheim ] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event

            let! event = getEvent authenticatedClient createdEvent.Event.Id

            Assert.True(Result.isOk event)
            let actual = event |> Result.map (fun e -> e.Offices)
            match actual with
            | Ok offices ->
                Assert.True(offices.IsSome)
                Assert.Equal(2, List.length offices.Value)
                let first::second::_ = offices.Value
                Assert.Equal(first, Oslo)
                Assert.Equal(second, Trondheim)
            | Error e -> failwith e
        }
    [<Fact>]
    member _.``Create event without office works and string representation of Office is empty``() =
        let event = TestData.createEvent (fun e -> { e with Offices = None })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event

            let! event = getEvent authenticatedClient createdEvent.Event.Id

            match event with
            | Ok event -> Assert.True(event.Offices.IsNone)
            | Error e -> failwith e
        }