namespace Tests.GetEvent

open Xunit
open System.Net

open Models
open Tests

[<Collection("Database collection")>]
type GetEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    let clientDifferentUserAdmin =
        fixture.getAuthedClientWithClaims 40 [ "admin:arrangement" ]

    [<Fact>]
    member _.``Anyone can get event id by shortname``() =
        let shortname =
            Generator.generateRandomString ()

        let event =
            TestData.createEvent (fun e -> { e with Shortname = Some shortname })

        task {
            let! _, createdEvent = Helpers.createEvent authenticatedClient event
            let! _, content = Http.getEventIdByShortname unauthenticatedClient shortname
            useCreatedEvent createdEvent (fun createdEvent -> Assert.Equal($"\"{createdEvent.Event.Id}\"", content))
        }

    [<Fact>]
    member _.``External events can be seen by anyone``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.Event.Id}"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Internal events cannot be seen if not authenticated``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.Event.Id}"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Internal events can be seen if authenticated``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get authenticatedClient $"/events/{createdEvent.Event.Id}"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Participants can be counted by anyone if event is external``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.Event.Id}/participants/count"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Participants can not be counted by external if event is internal``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.Event.Id}/participants/count"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Participants can be counted by authorized user if event is internal``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get authenticatedClient $"/events/{createdEvent.Event.Id}/participants/count"
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
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event

            for _ in 0..4 do
                let! _ = Helpers.createParticipant authenticatedClient createdEvent.Event
                ()

            let! _, content = Http.get authenticatedClient $"/events/{createdEvent.Event.Id}/participants/count"
            Assert.Equal("5", content)
        }

    [<Fact>]
    member _.``Unauthenticated user can get waitlist spot when event is external``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! created = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! response, _ =
                Http.get
                    unauthenticatedClient
                    $"/events/{createdEvent.Event.Id}/participants/{created.Email}/waitinglist-spot"

            response.EnsureSuccessStatusCode() |> ignore
        }


    [<Fact>]
    member _.``Unauthenticated user can not get waitlist spot when event is internal``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! created = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! response, _ =
                Http.get
                    unauthenticatedClient
                    $"/events/{createdEvent.Event.Id}/participants/{created.Email}/waitinglist-spot"

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated user can get waitlist spot when event is external``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! created = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! response, _ =
                Http.get
                    authenticatedClient
                    $"/events/{createdEvent.Event.Id}/participants/{created.Email}/waitinglist-spot"

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
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! created = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! _, content =
                Http.get
                    authenticatedClient
                    $"/events/{createdEvent.Event.Id}/participants/{created.Email}/waitinglist-spot"

            Assert.Equal("1", content)
        }

    [<Fact>]
    member _.``Unauthenticated user cannot get CSV export``() =
        let event = Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.Event.Id}/participants/export"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated user cannot get CSV export``() =
        let event = Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.Event.Id}/participants/export"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Admin user can get CSV export``() =
        let event = Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get clientDifferentUserAdmin $"/events/{createdEvent.Event.Id}/participants/export"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Can get CSV export with edit token``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event

            let! response, _ =
                Http.getCsvWithEditToken unauthenticatedClient createdEvent.Event.Id createdEvent.EditToken

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get future events``() =
        task {
            let! response, _ = Http.get unauthenticatedClient "/events"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

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
    member _.``Admin can get events and participations for different users``() =
        task {
            let! response, _ = Http.get clientDifferentUserAdmin "/events-and-participations/0"
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated users cannot get participants for event``() =
        let event =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = false
                    MaxParticipants = Some 3
                    HasWaitingList = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! response, _ = Http.get unauthenticatedClient $"/events/{createdEvent.Event.Id}/participants"
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated users can get participants for event``() =
        let event =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = false
                    MaxParticipants = Some 3
                    HasWaitingList = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event

            for _ in 0..7 do
                let! _ = Helpers.createParticipant authenticatedClient createdEvent.Event
                ()

            let! response, result = Helpers.getParticipationsAndWaitlist authenticatedClient createdEvent.Event.Id
            Assert.Equal(List.length result.Attendees, 3)
            Assert.Equal(List.length result.WaitingList, 5)
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Authenticated users can get its own participations``() =
        task {
            let mutable eventIds = []

            for _ in 0..4 do
                let event =
                    TestData.createEvent (fun e ->
                        { e with
                            IsExternal = false
                            MaxParticipants = Some 3
                            HasWaitingList = true })

                let! _, createdEvent = Helpers.createEvent authenticatedClient event
                let createdEvent = getCreatedEvent createdEvent
                eventIds <- List.append eventIds [ createdEvent.Event.Id ]

                let email = Generator.generateEmail ()
                let participant = Generator.generateParticipant email createdEvent.Event
                let! _ = Helpers.createParticipantForEvent authenticatedClient createdEvent.Event.Id email participant
                ()

            let! response, content = Helpers.getParticipationsForEmployee authenticatedClient 0
            let filteredParticipations =
                List.filter (fun eventId -> List.contains eventId eventIds) content.Participations
            
            Assert.Equal(5, List.length filteredParticipations)

            response.EnsureSuccessStatusCode() |> ignore
        }

     [<Fact>]
     member _.``Unauthenticated users can get publicly available event info``() =
        task {
            let! response, _ = Http.get unauthenticatedClient $"/events/public"
            Assert.Equal(HttpStatusCode.OK, response.StatusCode)
        }

    [<Fact>]
     member _.``Public events does not return social events``() =
        task {
            for _ in 0..4 do
                let event =
                    TestData.createEvent (fun e ->
                        { e with
                            IsExternal = false
                            EventType = Sosialt })
    
                let! _, _ = Helpers.createEvent authenticatedClient event
                ()
    
            let fagligEvent = TestData.createEvent (fun e ->
                { e with
                    IsExternal = false
                    EventType = Faglig})
    
            let! _, _ = Helpers.createEvent authenticatedClient fagligEvent
    
            let! _, body = Helpers.getPublicEvents unauthenticatedClient
    
            Assert.True(List.forall (fun (event: EventSummary) -> event.EventType = Faglig) body)
        }
    
    
    [<Fact>]
    member _.``Public events returns only publicly available events``() =
        task {
            for _ in 0..4 do
                let event =
                    TestData.createEvent (fun e ->
                        { e with
                            IsExternal = false
                            EventType = Sosialt })
    
                let! _, _ = Helpers.createEvent authenticatedClient event
                ()
    
            let fagligEvent = TestData.createEvent (fun e ->
                { e with
                    IsExternal = false
                    EventType = Faglig})
    
            let! _, _ = Helpers.createEvent authenticatedClient fagligEvent
    
            let! _, body = Helpers.getPublicEvents unauthenticatedClient
    
            Assert.True(List.forall (fun (event: EventSummary) -> event.IsPubliclyAvailable = true) body)
        }
