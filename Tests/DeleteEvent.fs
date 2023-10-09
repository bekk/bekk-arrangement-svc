namespace Tests.DeleteEvent

open System.Net
open Xunit

open Models
open Tests
open Email.Service

[<Collection("Database collection")>]
type DeleteEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    let isAvmeldtEmail (email: DevEmail) = email.Email.Message.Contains "Vi bekrefter at du nå er avmeldt"
    let participantIsAvmeldt (email: DevEmail) = email.Email.Message.Contains "har meldt seg av"

    let clientDifferentUserAdmin =
        fixture.getAuthedClientWithClaims 40 [ "admin:arrangement" ]

    [<Fact>]
    member _.``Unauthenticated user with cannot delete event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! response, _ = Http.deleteEvent unauthenticatedClient createdEvent.Event.Id
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated user who created event should be able to delete it``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ = Http.deleteEvent authenticatedClient createdEvent.Event.Id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Admins can delete event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ = Http.deleteEvent clientDifferentUserAdmin createdEvent.Event.Id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated user with edit token should be able to delete event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ =
                Http.deleteEventWithEditToken unauthenticatedClient createdEvent.Event.Id createdEvent.EditToken

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated user with cannot cancel event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! response, _ = Http.cancelEvent unauthenticatedClient createdEvent.Event.Id
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Admins can cancel event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ = Http.cancelEvent clientDifferentUserAdmin createdEvent.Event.Id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Authenticated user who created event should be able to cancel it``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ = Http.cancelEvent authenticatedClient createdEvent.Event.Id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated user with edit token should be able to cancel event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ =
                Http.cancelEventWithEditToken unauthenticatedClient createdEvent.Event.Id createdEvent.EditToken

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Admins can delete participants``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    MaxParticipants = Some 1
                    ParticipantQuestions = [] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! participant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! response, _ =
                Http.deleteParticipantFromEvent clientDifferentUserAdmin createdEvent.Event.Id participant.Email

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Delete participant using cancellation token``() =
        let generatedEvent =
            TestData.createEvent (fun e -> { e with ParticipantQuestions = [] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! participant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! response, _ =
                Http.deleteParticipantFromEventWithCancellationToken
                    authenticatedClient
                    createdEvent.Event.Id
                    participant.Email
                    participant.CreatedModel.CancellationToken

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Delete participant using edit token``() =
        let generatedEvent =
            TestData.createEvent (fun e -> { e with ParticipantQuestions = [] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! participant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! response, _ =
                Http.deleteParticipantFromEventWithEditToken
                    authenticatedClient
                    createdEvent.Event.Id
                    participant.Email
                    createdEvent.EditToken

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Deleting participant should delete participant``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    HasWaitingList = true
                    ParticipantQuestions = [] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! participant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! _, contentBeforeDelete =
                Http.get authenticatedClient $"/events/{createdEvent.Event.Id}/participants/count"

            let! _, _ =
                Http.deleteParticipantFromEventWithCancellationToken
                    authenticatedClient
                    createdEvent.Event.Id
                    participant.Email
                    participant.CreatedModel.CancellationToken

            let! _, contentAfterDelete =
                Http.get authenticatedClient $"/events/{createdEvent.Event.Id}/participants/count"

            Assert.Equal(contentBeforeDelete, "1")
            Assert.Equal(contentAfterDelete, "0")

        }

    [<Fact>]
    member _.``Deleting participant should update waitlist``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    HasWaitingList = true
                    MaxParticipants = Some 1
                    ParticipantQuestions = [] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! firstParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event
            let! secondParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! _, secondParticipantSpot =
                Http.get
                    authenticatedClient
                    $"/events/{createdEvent.Event.Id}/participants/{secondParticipant.Email}/waitinglist-spot"

            Assert.Equal(secondParticipantSpot, "1")

            let! deleteResponse, _ =
                Http.deleteParticipantFromEventWithCancellationToken
                    authenticatedClient
                    createdEvent.Event.Id
                    firstParticipant.Email
                    firstParticipant.CreatedModel.CancellationToken

            deleteResponse.EnsureSuccessStatusCode() |> ignore

            let! _, secondParticipantSpot =
                Http.get
                    authenticatedClient
                    $"/events/{createdEvent.Event.Id}/participants/{secondParticipant.Email}/waitinglist-spot"

            Assert.Equal(secondParticipantSpot, "0")

            let! _, participantCount =
                Http.get authenticatedClient $"/events/{createdEvent.Event.Id}/participants/count"

            Assert.Equal(participantCount, "1")
        }

    [<Fact>]
    member _.``Deleting a participant should generate 2 emails``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    MaxParticipants = Some 1
                    ParticipantQuestions = [] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! participant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            emptyDevMailbox ()
            let! _ = Http.deleteParticipantFromEvent clientDifferentUserAdmin createdEvent.Event.Id participant.Email
            let mailbox = getDevMailbox ()

            Assert.Equal(2, List.length mailbox)
            Assert.True(List.exists isAvmeldtEmail mailbox)
            Assert.True(List.exists participantIsAvmeldt mailbox)
        }

    [<Fact>]
    member _.``Deleting a participant on waitinglist should generate 1 email``() =
        let generatedEvent =
            TestData.createEvent(fun e ->
                { e with
                    HasWaitingList = true
                    MaxParticipants = Some 0
                    ParticipantQuestions = []})

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! participant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            emptyDevMailbox()
            let! _ = Http.deleteParticipantFromEvent clientDifferentUserAdmin createdEvent.Event.Id participant.Email
            let mailbox = getDevMailbox()

            Assert.Equal(1, List.length mailbox)
            Assert.True(List.exists isAvmeldtEmail mailbox)
            Assert.False(List.exists participantIsAvmeldt mailbox)
        }

    [<Fact>]
    member _.``Deleting participant that does not exist returns 404``() =
        let generatedEvent =
            TestData.createEvent (fun e ->
                { e with
                    IsExternal = true
                    HasWaitingList = true
                    ParticipantQuestions = [] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! _, _ =
                Http.get authenticatedClient $"/events/{createdEvent.Event.Id}/participants/count"

            let! result, _ =
                Http.deleteParticipantFromEventWithCancellationToken
                    authenticatedClient
                    createdEvent.Event.Id
                    "foo@email"
                    ""

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode)
        }