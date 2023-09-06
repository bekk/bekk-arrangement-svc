namespace Tests.SendMailOnUpdateEvent

open System
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

            let! createdEvent = Helpers.createEventAndGet authenticatedClient generatedEvent

            let! createdParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id

            emptyDevMailbox ()

            let! updatedEvent =
                Helpers.updateEventAndGet authenticatedClient createdEvent.Event.Id (updateMap generatedEvent)

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
                        Shortname = Some <| Generator.generateName()
                        Title = Generator.generateName () })

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
                $" endret lokasjon fra {data.CreatedEvent.Event.Location} til {data.UpdatedEvent.Location}"

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
                        OrganizerName = "ORGANIZER_" + Generator.generateName ()
                        OrganizerEmail = "ORGANIZER_EMAIL_" + Generator.generateEmail () })

            let actual =
                data.Message.Value.Email.Message

            Assert.Contains(data.UpdatedEvent.OrganizerName, actual)
            Assert.Contains(data.UpdatedEvent.OrganizerEmail, actual)
            Assert.False(actual.Contains(data.CreatedEvent.Event.OrganizerName))
            Assert.False(actual.Contains(data.CreatedEvent.Event.OrganizerEmail))
        }


    [<Fact>]
    member _.``Changing something in old event does not send email``() =
        task {
            let! data =
                init (fun ev ->
                    { ev with
                        StartDate = Generator.generateDateTimeCustomPast()
                        Location = ev.Location + "_NEW_LOCATION" // just to trigger email
                        OrganizerName = "ORGANIZER_" + Generator.generateName ()
                        OrganizerEmail = "ORGANIZER_EMAIL_" + Generator.generateEmail () })

            Assert.Equal(data.Message, None)
        }
