namespace Tests.RegisterToEvent

open Xunit
open System.Net

open Models
open Tests

[<Collection("Database collection")>]
type RegisterToEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient ()

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient ()

    [<Fact>]
    member _.``Unauthenticated user can join external event``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    MaxParticipants = Some 1 })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent
            let! response, _ = Helpers.createParticipant unauthenticatedClient createdEvent.event.id
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
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent
            let! response, _ = Helpers.createParticipant unauthenticatedClient createdEvent.event.id
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
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent
            let! response, _ = Helpers.createParticipant authenticatedClient createdEvent.event.id
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
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent
            let! response, _ = Helpers.createParticipant authenticatedClient createdEvent.event.id
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
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent
            let! _ = Http.cancelEvent authenticatedClient createdEvent.event.id
            let! _, unauthenticatedResponseBody = Helpers.createParticipant unauthenticatedClient createdEvent.event.id
            let! _, authenticatedResponseBody = Helpers.createParticipant authenticatedClient createdEvent.event.id

            Models.useUserMessage unauthenticatedResponseBody (fun userMessage ->
                Assert.Equal("Arrangementet er kansellert", userMessage.userMessage))

            Models.useUserMessage authenticatedResponseBody (fun userMessage ->
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
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent
            let! _, unauthenticatedResponseBody = Helpers.createParticipant unauthenticatedClient createdEvent.event.id
            let! _, authenticatedResponseBody = Helpers.createParticipant authenticatedClient createdEvent.event.id

            Models.useUserMessage unauthenticatedResponseBody (fun userMessage ->
                Assert.Equal("Arrangementet tok sted i fortiden", userMessage.userMessage))

            Models.useUserMessage authenticatedResponseBody (fun userMessage ->
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
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent
            let! _, unauthenticatedResponseBody = Helpers.createParticipant unauthenticatedClient createdEvent.event.id
            let! _, authenticatedResponseBody = Helpers.createParticipant authenticatedClient createdEvent.event.id

            Models.useUserMessage unauthenticatedResponseBody (fun userMessage ->
                Assert.Equal("Arrangementet har ikke plass", userMessage.userMessage))

            Models.useUserMessage authenticatedResponseBody (fun userMessage ->
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
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let createdEvent = getEvent createdEvent
            let! unauthenticatedResponse, _ = Helpers.createParticipant unauthenticatedClient createdEvent.event.id
            let! authenticatedResponse, _ = Helpers.createParticipant authenticatedClient createdEvent.event.id

            unauthenticatedResponse.EnsureSuccessStatusCode()
            |> ignore

            authenticatedResponse.EnsureSuccessStatusCode()
            |> ignore
        }
