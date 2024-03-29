namespace Tests.UpdateEvent

open System.Net
open Xunit

open Models
open Tests
open Email.Service

[<Collection("Database collection")>]
type UpdateEvent(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    let isPaameldt (email: DevEmail) = email.Email.Message.Contains "Du har rykket opp fra ventelisten"

    let clientDifferentUserAdmin =
        fixture.getAuthedClientWithClaims 40 [ "admin:arrangement" ]

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
    member _.``Admin can edit events``() =
        let generatedEvent =
            Generator.generateEvent ()

        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let eventToUpdate =
                { generatedEvent with Title = "This is a new title!" }

            let! response, updatedEvent = Helpers.updateEvent clientDifferentUserAdmin createdEvent.Event.Id eventToUpdate

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
                    { generatedEvent with MaxParticipants = None }

            response.EnsureSuccessStatusCode() |> ignore
        }

    [<Fact>]
    member _.``Changing max-participants to a lower amount than people participating is not possible``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with MaxParticipants = Some 1 })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.Event

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
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.Event

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
            let! _ = Helpers.createParticipant authenticatedClient createdEvent.Event

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
    member _.``Increasing max-participants by N will give N top people from waitinglist space and sends them emails``() =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with MaxParticipants = Some 0; HasWaitingList = true; StartDate = Generator.generateDateTimeCustomFuture() })
            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! firstParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event
            let! secondParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event
            let! thirdParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event

            let! firstParticipantSpot = Helpers.getParticipantWaitlistSpot authenticatedClient createdEvent.Event.Id firstParticipant.Email
            let! secondParticipantSpot = Helpers.getParticipantWaitlistSpot authenticatedClient createdEvent.Event.Id secondParticipant.Email
            let! thirdParticipantSpot = Helpers.getParticipantWaitlistSpot authenticatedClient createdEvent.Event.Id thirdParticipant.Email

            // Assert that they are on waitinglist
            Assert.Equal("1", firstParticipantSpot)
            Assert.Equal("2", secondParticipantSpot)
            Assert.Equal("3", thirdParticipantSpot)

            emptyDevMailbox()

            let! response, _ =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with MaxParticipants = Some 2 }
            
            response.EnsureSuccessStatusCode() |> ignore

            let! firstParticipantSpot = Helpers.getParticipantWaitlistSpot authenticatedClient createdEvent.Event.Id firstParticipant.Email
            let! secondParticipantSpot = Helpers.getParticipantWaitlistSpot authenticatedClient createdEvent.Event.Id secondParticipant.Email
            let! thirdParticipantSpot = Helpers.getParticipantWaitlistSpot authenticatedClient createdEvent.Event.Id thirdParticipant.Email

            // Assert that two are no longer on waitlinglist
            Assert.Equal("0", firstParticipantSpot)
            Assert.Equal("0", secondParticipantSpot)
            // Assert that the third participant is first in line
            Assert.Equal("1", thirdParticipantSpot)

            let mailbox = getDevMailbox()

            Assert.Equal(2, List.length mailbox)
            Assert.True(List.forall isPaameldt mailbox)
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
            let! _ = Helpers.createParticipantwithQuestions authenticatedClient createdEvent.Event

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

    static member CanChangeOfficeData =
        [
            [| Oslo; Trondheim |]
            [| Trondheim; Oslo |]
        ]
    [<Theory>]
    [<MemberData(nameof(UpdateEvent.CanChangeOfficeData))>]
    member _.``Can change office`` fromOffice toOffice =
        task {
            let generatedEvent =
                TestData.createEvent (fun e -> { e with Offices = Some [ fromOffice ] })

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! response, _ =
                Helpers.updateEvent
                    authenticatedClient
                    createdEvent.Event.Id
                    { generatedEvent with Offices = Some [ toOffice ]}

            response.EnsureSuccessStatusCode() |> ignore
        }

        // TODO CHANGE TO NO OFFICE
