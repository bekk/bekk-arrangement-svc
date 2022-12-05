namespace Tests.RegisterToEvent

open Xunit
open System.Net

open Models
open Tests

[<Collection("Database collection")>]
type RegisterToEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    [<Fact>]
    member _.``Unauthenticated user can join external event``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    MaxParticipants = Some 1 })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! response, _ = Helpers.createParticipant unauthenticatedClient createdEvent.Event.Id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated user can not join internal event``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = false
                    MaxParticipants = Some 1 })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! response, _ = Helpers.createParticipant unauthenticatedClient createdEvent.Event.Id
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated user can join external event``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    MaxParticipants = Some 1 })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! response, _ = Helpers.createParticipant authenticatedClient createdEvent.Event.Id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Authenticated user can join internal event``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = false
                    MaxParticipants = Some 1 })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! response, _ = Helpers.createParticipant authenticatedClient createdEvent.Event.Id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``No-one can join cancelled event``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    HasWaitingList = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! _ = Http.cancelEvent authenticatedClient createdEvent.Event.Id
            let! _, unauthenticatedResponseBody = Helpers.createParticipant unauthenticatedClient createdEvent.Event.Id
            let! _, authenticatedResponseBody = Helpers.createParticipant authenticatedClient createdEvent.Event.Id

            useUserMessage unauthenticatedResponseBody (fun userMessage ->
                Assert.Equal("Arrangementet er kansellert", userMessage.userMessage))

            useUserMessage authenticatedResponseBody (fun userMessage ->
                Assert.Equal("Arrangementet er kansellert", userMessage.userMessage))
        }

    [<Fact>]
    member _.``No-one can join event in the past``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    EndDate = Generator.generateDateTimeCustomPast ()
                    IsExternal = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! _, unauthenticatedResponseBody = Helpers.createParticipant unauthenticatedClient createdEvent.Event.Id
            let! _, authenticatedResponseBody = Helpers.createParticipant authenticatedClient createdEvent.Event.Id

            useUserMessage unauthenticatedResponseBody (fun userMessage ->
                Assert.Equal("Arrangementet tok sted i fortiden", userMessage.userMessage))

            useUserMessage authenticatedResponseBody (fun userMessage ->
                Assert.Equal("Arrangementet tok sted i fortiden", userMessage.userMessage))
        }

    [<Fact>]
    member _.``No-one can join full event``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    MaxParticipants = Some 0
                    HasWaitingList = false })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! _, unauthenticatedResponseBody = Helpers.createParticipant unauthenticatedClient createdEvent.Event.Id
            let! _, authenticatedResponseBody = Helpers.createParticipant authenticatedClient createdEvent.Event.Id

            useUserMessage unauthenticatedResponseBody (fun userMessage ->
                Assert.Equal("Arrangementet har ikke plass", userMessage.userMessage))

            useUserMessage authenticatedResponseBody (fun userMessage ->
                Assert.Equal("Arrangementet har ikke plass", userMessage.userMessage))
        }

    [<Fact>]
    member _.``Anyone can join full event if it has a waitinglist``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    MaxParticipants = Some 0
                    HasWaitingList = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! unauthenticatedResponse, _ = Helpers.createParticipant unauthenticatedClient createdEvent.Event.Id
            let! authenticatedResponse, _ = Helpers.createParticipant authenticatedClient createdEvent.Event.Id

            unauthenticatedResponse.EnsureSuccessStatusCode()
            |> ignore

            authenticatedResponse.EnsureSuccessStatusCode()
            |> ignore
        }
