namespace Tests.DeleteEvent

open System.Net
open Xunit

open Models
open Tests

[<Collection("Database collection")>]
type DeleteEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    let clientWithAdminToken =
        fixture.getAuthedClientWithClaims [ "admin:arrangement" ]

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
            let! participant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id

            let! response, _ =
                Http.deleteParticipantFromEvent clientWithAdminToken createdEvent.Event.Id participant.Email

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Delete participant using cancellation token``() =
        let generatedEvent =
            TestData.createEvent (fun e -> { e with ParticipantQuestions = [] })

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! participant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id

            let! response, _ =
                Http.deleteParticipantFromEventWithCancellationToken
                    authenticatedClient
                    createdEvent.Event.Id
                    participant.Email
                    participant.CreatedModel.CancellationToken

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
            let! participant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id

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
            let! firstParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id
            let! secondParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id

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
