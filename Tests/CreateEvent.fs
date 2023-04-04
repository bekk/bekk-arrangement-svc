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
            let actual = event |> Result.map (fun e -> e.Offices)
            match actual with
            | Ok office ->
                Assert.Single(office) |> ignore
                Assert.Equal(List.head office, Trondheim)
            | Error e -> failwith e
        }
    [<Fact>]
    member _.``Create event with two offices``() =
        let event =
            TestData.createEvent (fun e -> { e with Offices = [ Oslo; Trondheim ] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event

            let! event = getEvent authenticatedClient createdEvent.Event.Id

            Assert.True(Result.isOk event)
            let actual = event |> Result.map (fun e -> e.Offices)
            match actual with
            | Ok offices ->
                Assert.Equal(2, List.length offices)
                let first::second::_ = offices
                Assert.Equal(first, Oslo)
                Assert.Equal(second, Trondheim)
            | Error e -> failwith e
        }
    [<Fact>]
    member _.``Create event without office works and string representation of Office is empty``() =
        let event = TestData.createEvent (fun e -> { e with Offices = [] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event

            let! event = getEvent authenticatedClient createdEvent.Event.Id

            match event with
            | Ok event ->
                Assert.Single(event.Offices) |> ignore
                Assert.Equal(List.head event.Offices, Annet)
                let encodedOffice = Models.Office.encoder (List.head event.Offices)
                Assert.Equal("", encodedOffice.ToString())
            | Error e -> failwith e
        }