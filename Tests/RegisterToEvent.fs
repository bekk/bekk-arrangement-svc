namespace Tests.RegisterToEvent

open Xunit
open System.Net

open Models
open Tests
open Email.Service

[<Collection("Database collection")>]
type RegisterToEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    let isParticipatingEmail (email: DevEmail) = email.Email.Message.Contains "Du er nå påmeldt"
    let isWaitlistedEmail (email: DevEmail) = email.Email.Message.Contains "Du er nå på venteliste"

    [<Fact>]
    member _.``Unauthenticated user can join external event``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    MaxParticipants = Some 1 })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! response, _ = Helpers.createParticipant unauthenticatedClient createdEvent.Event
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
            let! response, _ = Helpers.createParticipant unauthenticatedClient createdEvent.Event
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
            let! response, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
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
            let! response, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
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
            let! _, unauthenticatedResponseBody = Helpers.createParticipant unauthenticatedClient createdEvent.Event
            let! _, authenticatedResponseBody = Helpers.createParticipant authenticatedClient createdEvent.Event

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
            let! _, unauthenticatedResponseBody = Helpers.createParticipant unauthenticatedClient createdEvent.Event
            let! _, authenticatedResponseBody = Helpers.createParticipant authenticatedClient createdEvent.Event

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
            let! _, unauthenticatedResponseBody = Helpers.createParticipant unauthenticatedClient createdEvent.Event
            let! _, authenticatedResponseBody = Helpers.createParticipant authenticatedClient createdEvent.Event

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
            let! unauthenticatedResponse, _ = Helpers.createParticipant unauthenticatedClient createdEvent.Event
            let! authenticatedResponse, _ = Helpers.createParticipant authenticatedClient createdEvent.Event

            unauthenticatedResponse.EnsureSuccessStatusCode()
            |> ignore

            authenticatedResponse.EnsureSuccessStatusCode()
            |> ignore
        }

    [<Fact>]
    member _.``Email gets sent when registering``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    MaxParticipants = None })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            // Clear the mailbox after the event was created
            emptyDevMailbox ()
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event

            let mailbox = getDevMailbox ()

            Assert.Equal(1, List.length mailbox)
        }

    [<Fact>]
    member _.``Emails sent when event is full is waitlist email``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with MaxParticipants = Some 0 })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            // Clear the mailbox after the event was created
            emptyDevMailbox ()
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event

            let mailbox = getDevMailbox ()

            Assert.True(List.forall isWaitlistedEmail mailbox)
        }

    [<Fact>]
    member _.``Emails sent when event is full has the correct amount of participating and waitlisted emails``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with MaxParticipants = Some 2
                         HasWaitingList = true })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            // Clear the mailbox after the event was created
            emptyDevMailbox ()
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event

            let mailbox = getDevMailbox ()

            let participating = List.filter isParticipatingEmail mailbox
            let waitlisted = List.filter isWaitlistedEmail mailbox

            Assert.Equal(5, List.length mailbox)
            Assert.Equal(2, List.length participating)
            Assert.Equal(3, List.length waitlisted)
        }

    [<Fact>]
    member _.``Emails with no max-participants and no waitinglist sends participating email``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with MaxParticipants = None
                         HasWaitingList = false })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            // Clear the mailbox after the event was created
            emptyDevMailbox ()
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            let! _, _ = Helpers.createParticipant authenticatedClient createdEvent.Event

            let mailbox = getDevMailbox ()

            Assert.True(List.forall isParticipatingEmail mailbox)
        }

    [<Fact>]
    member _.``Registering participant saves answers correctly``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with MaxParticipants = None
                         HasWaitingList = false
                         ParticipantQuestions = [ { Id = None; Question = "Question 0" }
                                                  { Id = None; Question = "Question 1" }
                                                  { Id = None; Question = "Question 2" }
                                                  { Id = None; Question = "Question 3" }
                                                  { Id = None; Question = "Question 4" } ]
                })

        let email = Generator.generateEmail()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let participant =
                let generated = Generator.generateParticipant email createdEvent.Event
                { generated with ParticipantAnswers = List.mapi (fun index answer -> {answer with Answer = $"Answer {index}" }) generated.ParticipantAnswers }

            let! _, _ = Helpers.createParticipantForEvent authenticatedClient createdEvent.Event.Id email participant
            let! _, body = Helpers.getParticipationsAndWaitlist authenticatedClient createdEvent.Event.Id

            let actual =
                body.Attendees
                |> List.collect (fun qa -> qa.QuestionAndAnswers)
                |> List.mapi (fun index qa -> index, qa)
                |> List.forall (fun (index, qa) -> qa.Answer = $"Answer {index}" && qa.Question = $"Question {index}")

            Assert.True(actual)
        }

    [<Fact>]
    member _.``Registering participant without answering questions is OK ``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with MaxParticipants = None
                         HasWaitingList = false
                         ParticipantQuestions = [ { Id = None; Question = "Question 0" }
                                                  { Id = None; Question = "Question 1" }
                                                  { Id = None; Question = "Question 2" }
                                                  { Id = None; Question = "Question 3" }
                                                  { Id = None; Question = "Question 4" } ]
                })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! response, _ = Helpers.createParticipant authenticatedClient createdEvent.Event
            response.EnsureSuccessStatusCode() |> ignore
        }
