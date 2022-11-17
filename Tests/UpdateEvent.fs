namespace Tests.UpdateEvent

open System.Net
open Xunit

open Models
open Tests


// TODO: Legg til tester på legge til, sletting og endring av spørsmål
// TODO: Se på alle de mulige tingene som kan feile når man oppdaterer et event
[<Collection("Database collection")>]
type UpdateEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    [<Fact>]
    member _.``Edit event without without authorization gives forbidden``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let eventToUpdate =
                { generatedEvent with Title = "This is a new title!" }

            let! response, _ = Http.updateEvent unauthenticatedClient createdEvent.Event.Id eventToUpdate
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode)
        }

    [<Fact>]
    member _.``Edit event with authorization works``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let eventToUpdate =
                { generatedEvent with Title = "This is a new title!" }

            let! response, updatedEvent = Helpers.updateEvent authenticatedClient createdEvent.Event.Id eventToUpdate

            let updatedEvent =
                getUpdatedEvent updatedEvent

            Assert.Equal("This is a new title!", updatedEvent.Title)
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Edit event without authorization but with edit token works``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let eventToUpdate =
                { generatedEvent with Title = "This is a new title!" }

            let! response, updatedEvent =
                Helpers.updateEventWithEditTokenTest
                    unauthenticatedClient
                    createdEvent.Event.Id
                    createdEvent.EditToken
                    eventToUpdate

            Assert.Equal("This is a new title!", updatedEvent.Title)
            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Can change max-participants from anything when new value is infinite``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with MaxParticipants = Some 10 })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    ({ generatedEvent with MaxParticipants = None })

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Changing max-participants to a lower amount than people participating is not possible``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with MaxParticipants = Some 1 })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.Event.Id

            let! _, result =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with MaxParticipants = Some 0 }

            useUserMessage result (fun userMessage ->
                Assert.Equal(
                    "Du kan ikke sette maks deltagere til lavere enn antall som allerede deltar",
                    userMessage.userMessage
                ))
        }

    [<Fact>]
    member _.``Changing max-participants to a lower amount is possible if there is room for it``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with MaxParticipants = Some 1 })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with MaxParticipants = Some 0 }

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Can not remove waiting list if someone is on it``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e ->
                    { e with
                        MaxParticipants = Some 0
                        HasWaitingList = true })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.Event.Id

            let! _, result =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with HasWaitingList = false }

            useUserMessage result (fun userMessage ->
                Assert.Equal("Du kan ikke fjerne venteliste når det er folk på den", userMessage.userMessage))
        }

    [<Fact>]
    member _.``If there was no max-participant limit, you cannot add one if its lower than number of participants``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with MaxParticipants = None })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.Event.Id

            let! _, result =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with MaxParticipants = Some 0 }

            useUserMessage result (fun userMessage ->
                Assert.Equal(
                    "Du kan ikke sette maks deltagere til lavere enn antall som allerede deltar",
                    userMessage.userMessage
                ))
        }

    [<Fact>]
    member _.``If there was no max-participant limit, you can add one if its greater than number of participants``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with MaxParticipants = None })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with MaxParticipants = Some 1 }

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Can change max-participants if the new value is greater than the old value``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with MaxParticipants = Some 0 })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with MaxParticipants = Some 1 }

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Can change questions if no one has answered any yet``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with ParticipantQuestions = Generator.generateQuestions 10 })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with ParticipantQuestions = [] }

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Can not change questions if someone has answered``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with ParticipantQuestions = Generator.generateQuestions 10 })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! _ = Helpers.createParticipantwithQuestions authenticatedClient createdEvent.Event.Id 10

            let! _, result =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with ParticipantQuestions = [] }

            useUserMessage result (fun userMessage ->
                Assert.Equal(
                    "Kan ikke endre på spørsmål som allerede har blitt stilt til deltakere",
                    userMessage.userMessage
                ))
        }
