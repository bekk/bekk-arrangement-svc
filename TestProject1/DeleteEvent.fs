namespace Tests.DeleteEvent

open System.Net
open Xunit

open Models
open Tests

[<Collection("Database collection")>]
type DeleteEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient ()

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient ()

    [<Fact>]
    member _.``Unauthenticated user with cannot delete event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let! response, _ = Http.deleteEvent unauthenticatedClient createdEvent.event.id
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated user who created event should be able to delete it``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent

            let! response, _ = Http.deleteEvent authenticatedClient createdEvent.event.id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated user with edit token should be able to delete event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent

            let! response, _ =
                Http.deleteEventWithEditToken unauthenticatedClient createdEvent.event.id createdEvent.editToken

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated user with cannot cancel event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let! response, _ = Http.cancelEvent unauthenticatedClient createdEvent.event.id
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Authenticated user who created event should be able to cancel it``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent

            let! response, _ = Http.cancelEvent authenticatedClient createdEvent.event.id
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Unauthenticated user with edit token should be able to cancel event``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent

            let! response, _ =
                Http.cancelEventWithEditToken unauthenticatedClient createdEvent.event.id createdEvent.editToken

            response.EnsureSuccessStatusCode() |> ignore
        }

    // TODO: Implement this when we can add admin claim to token
    // [<Fact>]
    // member _.``Admins can delete participants``() =
    //     let generatedEvent =
    //         Generator.generateEvent ()
    //
    //     task {
    //         let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
    //         let! _, participant = Helpers.createParticipant authenticatedClient createdEvent.event.id
    //         let! response, _ = Http.deleteParticipantFromEvent authenticatedClient createdEvent.event.id participant.Email
    //         response.EnsureSuccessStatusCode() |> ignore
    //     }

    [<Fact>]
    member _.``Delete participant using cancellation token``() =
        let generatedEvent =
            TestData.createEvent (fun e -> { e with ParticipantQuestions = [] })

        task {
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let! _, participant = Helpers.createParticipant authenticatedClient createdEvent.event.id

            match participant with
            | Participant participant ->
                let! response, _ =
                    Http.deleteParticipantFromEventWithCancellationToken
                        authenticatedClient
                        createdEvent.event.id
                        participant.Email
                        participant.CreatedModel.cancellationToken

                response.EnsureSuccessStatusCode() |> ignore
            | _ -> failwith "ASD"
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
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let! _, participant = Helpers.createParticipant authenticatedClient createdEvent.event.id

            match participant with
            | Participant participant ->
                let! _, contentBeforeDelete =
                    Http.get authenticatedClient $"/events/{createdEvent.event.id}/participants/count"

                let! _, _ =
                    Http.deleteParticipantFromEventWithCancellationToken
                        authenticatedClient
                        createdEvent.event.id
                        participant.Email
                        participant.CreatedModel.cancellationToken

                let! _, contentAfterDelete =
                    Http.get authenticatedClient $"/events/{createdEvent.event.id}/participants/count"

                Assert.Equal(contentBeforeDelete, "1")
                Assert.Equal(contentAfterDelete, "0")

            | _ -> failwith "Failed to get participant"
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
            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent
            let! _, firstParticipant = Helpers.createParticipant authenticatedClient createdEvent.event.id
            let! _, secondParticipant = Helpers.createParticipant authenticatedClient createdEvent.event.id

            match firstParticipant, secondParticipant with
            | Participant firstParticipant, Participant secondParticipant ->
                let! _, secondParticipantSpot =
                    Http.get
                        authenticatedClient
                        $"/events/{createdEvent.event.id}/participants/{secondParticipant.Email}/waitinglist-spot"

                Assert.Equal(secondParticipantSpot, "1")

                let! deleteResponse, _ =
                    Http.deleteParticipantFromEventWithCancellationToken
                        authenticatedClient
                        createdEvent.event.id
                        firstParticipant.Email
                        firstParticipant.CreatedModel.cancellationToken

                deleteResponse.EnsureSuccessStatusCode() |> ignore

                let! _, secondParticipantSpot =
                    Http.get
                        authenticatedClient
                        $"/events/{createdEvent.event.id}/participants/{secondParticipant.Email}/waitinglist-spot"

                Assert.Equal(secondParticipantSpot, "0")

                let! _, participantCount =
                    Http.get authenticatedClient $"/events/{createdEvent.event.id}/participants/count"

                Assert.Equal(participantCount, "1")
            | _, _ ->
                failwith "ASDA"
        }
