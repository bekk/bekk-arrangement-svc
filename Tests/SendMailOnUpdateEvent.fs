namespace Tests.SendMailOnUpdateEvent

open System.Net
open Tests
open Xunit

open Models
open Email.Service

type UpdateEventEmailTestResult =
    { CreatedEvent: CreatedEvent
      Participant: ParticipantTest
      UpdatedEvent: InnerEvent
      Message: DevEmail option }

[<Collection("Database collection")>]
type General(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient

    let unauthenticatedClient =
        fixture.getUnauthenticatedClient

    let init (updateMap: Models.EventWriteModel -> Models.EventWriteModel) =
        task {
            let start =
                DateTimeCustom
                    .now()
                    .ToDateTime()
                    .AddDays(1)
                    .ToDateTimeCustom()

            let generatedEvent =
                TestData.createEvent (fun e ->
                    { e with
                        StartDate = start
                        EndDate = start
                        IsExternal = true
                        MaxParticipants = None })

            let! _, createdEvent = Helpers.createEventTest authenticatedClient generatedEvent

            let createdEvent =
                getCreatedEvent createdEvent

            let! _, createdParticipant = Helpers.createParticipant authenticatedClient createdEvent.event.id

            let createdParticipant =
                getParticipant createdParticipant

            emptyDevMailbox ()

            let! _, updatedEvent =
                Helpers.updateEventTest authenticatedClient createdEvent.event.id (updateMap generatedEvent)

            let updatedEvent =
                getUpdatedEvent updatedEvent

            let devEmail =
                let mb = getDevMailbox () in

                if List.isEmpty mb then
                    None
                else
                    Some <| List.exactlyOne mb

            return
                { CreatedEvent = createdEvent
                  Participant = createdParticipant
                  UpdatedEvent = updatedEvent
                  Message = devEmail }
        }

    [<Fact>]
    member _.``Changing non-triggering fields do not send any emails``() =
        task {
            let! data =
                init (fun ev ->
                    { ev with
                        Description = Generator.generateRandomString ()
                        Shortname = Some <| Generator.generateRandomString ()
                        Title = Generator.generateRandomString () })

            Assert.Equal(None, data.Message)
        }

    [<Fact>]
    member _.``Changing start time``() =
        task {
            let! data =
                init (fun ev ->
                    { ev with
                        StartDate =
                            ev
                                .StartDate
                                .ToDateTime()
                                .AddHours(1)
                                .ToDateTimeCustom()
                        EndDate =
                            ev
                                .EndDate
                                .ToDateTime()
                                .AddHours(1)
                                .ToDateTimeCustom() })

            let actual =
                data.Message.Value.Email.Message

            let expected = " endret tidspunkt fra "
            Assert.Contains(expected, actual)
        }

    [<Fact>]
    member _.``Changing end time``() =
        task {
            let! data =
                init (fun ev ->
                    { ev with
                        EndDate =
                            ev
                                .EndDate
                                .ToDateTime()
                                .AddHours(1)
                                .ToDateTimeCustom() })

            let actual =
                data.Message.Value.Email.Message

            let expected = " endret tidspunkt fra "
            Assert.Contains(expected, actual)
        }

    [<Fact>]
    member _.``Changing location``() =
        task {
            let! data = init (fun ev -> { ev with Location = ev.Location + "_NEW_LOCATION" })

            let actual =
                data.Message.Value.Email.Message

            let expected =
                $" endret lokasjon fra {data.CreatedEvent.event.location} til {data.UpdatedEvent.location}"

            Assert.Contains(expected, actual)
        }

    [<Fact>]
    member _.``Changing location and time``() =
        task {
            let! data =
                init (fun ev ->
                    { ev with
                        Location = ev.Location + "_NEW_LOCATION"
                        StartDate =
                            ev
                                .StartDate
                                .ToDateTime()
                                .AddHours(1)
                                .ToDateTimeCustom()
                        EndDate =
                            ev
                                .EndDate
                                .ToDateTime()
                                .AddHours(1)
                                .ToDateTimeCustom() })

            let actual =
                data.Message.Value.Email.Message

            Assert.Contains("har endret tidspunkt og lokasjon", actual)
            Assert.Contains("- Tidspunkt er endret fra ", actual)
            Assert.Contains("- Lokasjon er endret fra ", actual)
        }

    [<Fact>]
    member _.``Changing organizer uses new organizer in message``() =
        task {
            let! data =
                init (fun ev ->
                    { ev with
                        Location = ev.Location + "_NEW_LOCATION" // just to trigger email
                        OrganizerName = "ORGANIZER_" + Generator.generateRandomString ()
                        OrganizerEmail = "ORGANIZER_EMAIL_" + Generator.generateEmail () })

            let actual =
                data.Message.Value.Email.Message

            Assert.Contains(data.UpdatedEvent.organizerName, actual)
            Assert.Contains(data.UpdatedEvent.organizerEmail, actual)
            Assert.False(actual.Contains(data.CreatedEvent.event.organizerName))
            Assert.False(actual.Contains(data.CreatedEvent.event.organizerEmail))
        }