namespace Tests.GetEvent

open Xunit
open System.Net

open Models
open Tests

[<Collection("Database collection")>]
type GetEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient ()

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient ()

    [<Fact>]
    member _.``Anyone can get event id by shortname``() =
        let shortname =
            Generator.generateRandomString ()

        let event =
            TestData.createEvent (fun e -> { e with Shortname = Some shortname })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! _, content = Http.getEventIdByShortname unauthenticatedClient shortname
            Assert.Equal($"\"{createdEvent.event.id}\"", content)
        }

    [<Fact>]
    member _.``External events can be seen by anyone``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.event.id}"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Internal events cannot be seen if not authenticated``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.event.id}"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Internal events can be seen if authenticated``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get authenticatedClient $"/events/{createdEvent.event.id}"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unfurl events can be seen by anyone``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.event.id}/unfurl"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Participants can be counted by anyone if event is external``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.event.id}/participants/count"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Participants can not be counted by external if event is internal``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.event.id}/participants/count"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Participants can be counted by authorized user if event is internal``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get authenticatedClient $"/events/{createdEvent.event.id}/participants/count"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Counting participants returns the correct number``() =
        let event =
            TestData.createEvent (fun e ->
                { e with
                    MaxParticipants = None
                    IsExternal = false })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event

            for _ in 0..4 do
                let! _ = Helpers.createParticipant authenticatedClient createdEvent.event.id
                ()

            let! _, content = Http.get authenticatedClient $"/events/{createdEvent.event.id}/participants/count"
            Assert.Equal("5", content)
        }

    [<Fact>]
    member _.``Unauthenticated user can get waitlist spot when event is external``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            // TODO: Create participant kan gjøres bedre
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! _, created = Helpers.createParticipant authenticatedClient createdEvent.event.id

            let! response, _ =
                Http.get
                    unauthenticatedClient
                    $"/events/{createdEvent.event.id}/participants/{created.Email}/waitinglist-spot"

            response.EnsureSuccessStatusCode() |> ignore
        }


    [<Fact>]
    member _.``Unauthenticated user can not get waitlist spot when event is internal``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            // TODO: Create participant kan gjøres bedre
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! _, created = Helpers.createParticipant authenticatedClient createdEvent.event.id

            let! response, _ =
                Http.get
                    unauthenticatedClient
                    $"/events/{createdEvent.event.id}/participants/{created.Email}/waitinglist-spot"

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated user can get waitlist spot when event is external``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            // TODO: Create participant kan gjøres bedre
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! _, created = Helpers.createParticipant authenticatedClient createdEvent.event.id

            let! response, _ =
                Http.get
                    authenticatedClient
                    $"/events/{createdEvent.event.id}/participants/{created.Email}/waitinglist-spot"

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Gets the correct waitlist spot``() =
        let event =
            TestData.createEvent (fun e ->
                { e with
                    HasWaitingList = true
                    MaxParticipants = Some 0
                    IsExternal = false })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! _, created = Helpers.createParticipant authenticatedClient createdEvent.event.id

            let! _, content =
                Http.get
                    authenticatedClient
                    $"/events/{createdEvent.event.id}/participants/{created.Email}/waitinglist-spot"

            Assert.Equal("1", content)
        }

    [<Fact>]
    member _.``Unauthenticated user cannot get CSV export``() =
        let event = Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.event.id}/participants/export"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    // TODO: Tester på at kun de som kan hente ut CVS får til det
    // TODO: Hent ut CSV med edit token

    [<Fact>]
    member _.``Unauthenticated users cannot get future events``() =
        task {
            let! response, _ = Http.get unauthenticatedClient "/events"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    // TODO: remove logging
    [<Fact>]
    member _.``Authenticated users can get future events``() =
        task {
            let! response, _ = Http.get authenticatedClient "/events"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get past events``() =
        task {
            let! response, _ = Http.get unauthenticatedClient "/events/previous"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get past events``() =
        task {
            let! response, _ = Http.get authenticatedClient "/events/previous"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get forside events``() =
        task {
            let email = Generator.generateEmail ()
            let! response, _ = Http.get unauthenticatedClient $"/events/forside/{email}"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get forside events``() =
        task {
            let email = Generator.generateEmail ()
            let! response, _ = Http.get authenticatedClient $"/events/forside/{email}"
            response.EnsureSuccessStatusCode() |> ignore
        }


    [<Fact>]
    member _.``Unauthenticated users cannot get events organized by id``() =
        task {
            let! response, _ = Http.get unauthenticatedClient "/events/organizer/0"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get events organized by id``() =
        task {
            let! response, _ = Http.get authenticatedClient "/events/organizer/0"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get events and participations``() =
        task {
            let! response, _ = Http.get unauthenticatedClient "/events-and-participations/0"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get events and participations``() =
        task {
            let! response, _ = Http.get authenticatedClient "/events-and-participations/0"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get participants for event``() =
        let event = TestData.createEvent (fun e -> { e with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true })
        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.event.id}/participants"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get participants for event``() =
        let event = TestData.createEvent (fun e -> { e with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true })
        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient event
            // TODO: FIX THIS
            for _ in 0..7 do
                let! _ = Helpers.createParticipant authenticatedClient createdEvent.event.id
                ()
            let! response, result = Http.getParticipationsAndWaitlist authenticatedClient createdEvent.event.id
            // TODO: FIX -> trekk ut
            match result with
            | Error e -> failwith $"Failed to decode participations and waitlist: {e}"
            | Ok result ->
                Assert.Equal(List.length result.attendees, 3)
                Assert.Equal(List.length result.waitingList, 5)

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get participations for participant``() =
        let email = Generator.generateEmail()
        task {
            let! response, _ = Http.get unauthenticatedClient $"/participants/{email}/events"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get participations for participant``() =
        task {
            let participant = Generator.generateParticipant 0
            let email = Generator.generateEmail()
            for _ in 0..4 do
                let event = TestData.createEvent (fun e -> { e with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true })
                let! _, createdEvent = Helpers.createEventTest authenticatedClient event
                let! _ = Helpers.createParticipantForEvent authenticatedClient createdEvent.event.id email participant
                ()

            // TODO: FIX
            let! response, content = Http.getParticipationsForEvent authenticatedClient email
            match content with
            | Error e -> return failwith $"Error decoding participaitons and answers: {e}"
            | Ok result ->
                Assert.Equal(List.length result, 5)

            response.EnsureSuccessStatusCode() |> ignore
        }
