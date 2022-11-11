namespace Tests.GetEvent

open Xunit
open System.Net

open Models
open Tests

module Helpers =
    // TODO: Denne hører egentlig ikke hjemme her føler jeg
    let createParticipant client eventId email participant =
        task {
            let! response, createdParticipant = Http.postParticipant client eventId email participant

            match createdParticipant with
            | Error e -> return failwith $"Unable to decode created participant: {e}"
            | Ok createdParticipant ->
                Assert.IsType<CreatedParticipant>(createdParticipant)
                |> ignore

                return response, createdParticipant
        }

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
            CreateEvent.TestData.createEvent (fun e -> { e with Shortname = Some shortname })

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            let! _, content = Http.getEventIdByShortname unauthenticatedClient shortname
            Assert.Equal($"\"{createdEvent.event.id}\"", content)
        }

    [<Fact>]
    member _.``External events can be seen by anyone``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            // TODO: /API/ er irriterende
            let! response, _ = Http.get unauthenticatedClient $"/api/events/{createdEvent.event.id}"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Internal events cannot be seen if not authenticated``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/api/events/{createdEvent.event.id}"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Internal events can be seen if authenticated``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get authenticatedClient $"/api/events/{createdEvent.event.id}"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unfurl events can be seen by anyone``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            // TODO: Denne kan trekkes ut
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/api/events/{createdEvent.event.id}/unfurl"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Participants can be counted by anyone if event is external``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/api/events/{createdEvent.event.id}/participants/count"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Participants can not be counted by external if event is internal``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/api/events/{createdEvent.event.id}/participants/count"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Participants can be counted by authorized user if event is internal``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get authenticatedClient $"/api/events/{createdEvent.event.id}/participants/count"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Counting participants returns the correct number``() =
        let event =
            CreateEvent.TestData.createEvent (fun e ->
                { e with
                    MaxParticipants = None
                    IsExternal = false })

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event

            for _ in 0..4 do
                let participant =
                    Generator.generateParticipant 0

                let email = Generator.generateEmail ()
                let! _ = Helpers.createParticipant authenticatedClient createdEvent.event.id email participant
                ()

            let! _, content = Http.get authenticatedClient $"/api/events/{createdEvent.event.id}/participants/count"
            Assert.Equal("5", content)
        }

    [<Fact>]
    member _.``Unauthenticated user can get waitlist spot when event is external``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            // TODO: Create participant kan gjøres bedre
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event

            let participant =
                Generator.generateParticipant 0

            let email = Generator.generateEmail ()
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.event.id email participant

            let! response, _ =
                Http.get
                    unauthenticatedClient
                    $"/api/events/{createdEvent.event.id}/participants/{email}/waitinglist-spot"

            response.EnsureSuccessStatusCode() |> ignore
        }


    [<Fact>]
    member _.``Unauthenticated user can not get waitlist spot when event is internal``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            // TODO: Create participant kan gjøres bedre
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event

            let participant =
                Generator.generateParticipant 0

            let email = Generator.generateEmail ()
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.event.id email participant

            let! response, _ =
                Http.get
                    unauthenticatedClient
                    $"/api/events/{createdEvent.event.id}/participants/{email}/waitinglist-spot"

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated user can get waitlist spot when event is external``() =
        let event =
            CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            // TODO: Create participant kan gjøres bedre
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event

            let participant =
                Generator.generateParticipant 0

            let email = Generator.generateEmail ()
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.event.id email participant

            let! response, _ =
                Http.get
                    authenticatedClient
                    $"/api/events/{createdEvent.event.id}/participants/{email}/waitinglist-spot"

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Gets the correct waitlist spot``() =
        let event =
            CreateEvent.TestData.createEvent (fun e ->
                { e with
                    HasWaitingList = true
                    MaxParticipants = Some 0
                    IsExternal = false })

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event

            let participant =
                Generator.generateParticipant 0

            let email = Generator.generateEmail ()
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.event.id email participant

            let! _, content =
                Http.get
                    authenticatedClient
                    $"/api/events/{createdEvent.event.id}/participants/{email}/waitinglist-spot"

            Assert.Equal("1", content)
        }

    [<Fact>]
    member _.``Unauthenticated user cannot get CSV export``() =
        let event =
            CreateEvent.TestData.createEvent id

        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/api/events/{createdEvent.event.id}/participants/export"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    // TODO: Tester på at kun de som kan hente ut CVS får til det
    // TODO: Hent ut CSV med edit token

    [<Fact>]
    member _.``Unauthenticated users cannot get future events``() =
        task {
            let! response, _ = Http.get unauthenticatedClient "/api/events"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    // TODO: remove logging
    [<Fact>]
    member _.``Authenticated users can get future events``() =
        task {
            let! response, _ = Http.get authenticatedClient "/api/events"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get past events``() =
        task {
            let! response, _ = Http.get unauthenticatedClient "/api/events/previous"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get past events``() =
        task {
            let! response, _ = Http.get authenticatedClient "/api/events/previous"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get forside events``() =
        task {
            let email = Generator.generateEmail ()
            let! response, _ = Http.get unauthenticatedClient $"/api/events/forside/{email}"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get forside events``() =
        task {
            let email = Generator.generateEmail ()
            let! response, _ = Http.get authenticatedClient $"/api/events/forside/{email}"
            response.EnsureSuccessStatusCode() |> ignore
        }


    [<Fact>]
    member _.``Unauthenticated users cannot get events organized by id``() =
        task {
            let! response, _ = Http.get unauthenticatedClient "/api/events/organizer/0"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get events organized by id``() =
        task {
            let! response, _ = Http.get authenticatedClient "/api/events/organizer/0"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get events and participations``() =
        task {
            let! response, _ = Http.get unauthenticatedClient "/api/events-and-participations/0"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get events and participations``() =
        task {
            let! response, _ = Http.get authenticatedClient "/api/events-and-participations/0"
            response.EnsureSuccessStatusCode() |> ignore
        }



    [<Fact>]
    member _.``Unauthenticated users cannot get participants for event``() =
        let event = CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true })
        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/api/events/{createdEvent.event.id}/participants"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get participants for event``() =
        let event = CreateEvent.TestData.createEvent (fun e -> { e with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true })
        task {
            let! _, createdEvent = CreateEvent.Helpers.createEventTest authenticatedClient event
            // TODO: FIX THIS
            for _ in 0..7 do
                let participant =
                    Generator.generateParticipant 0

                let email = Generator.generateEmail ()
                let! _ = Helpers.createParticipant authenticatedClient createdEvent.event.id email participant
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
