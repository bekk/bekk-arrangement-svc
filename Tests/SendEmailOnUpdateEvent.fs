module Tests.SendEmailOnUpdateEvent

open System.Threading.Tasks

open Expecto

open TestUtils
open Api
open Email.Service

type UpdateEventEmailTestResult = {
   OldEvent: CreatedEvent
   Participant: RegisteredParticipant
   NewEvent: CreatedEvent
   Message: DevEmail option
}

let tests =
  // We use sequenced tests as the dev mailbox is a shared resource. Running tests in parallel makes it difficult
  // to locate just mails for this test
  testSequenced <| testList "Update event sends email notification"  [
    let init (createMap: Models.EventWriteModel -> Models.EventWriteModel) (updateMap : Models.EventWriteModel -> Models.EventWriteModel) : UpdateEventEmailTestResult Task =
      task {
        let! oldEvent =
          // Having past events triggers other errors, as do End = now
          let start = DateTimeCustom.now().ToDateTime().AddDays(1).ToDateTimeCustom()
          TestData.createEvent (createMap << (fun ev -> { ev with StartDate = start; EndDate = start; IsExternal = true; MaxParticipants = None }))
        let! participant = justContent <| Participant.create UsingJwtToken.request oldEvent id (always "simen.endsjo@bekk.no")
        emptyDevMailbox()
        let! newEvent = justContent <| Events.update UsingJwtToken.request oldEvent updateMap
        let devEmail =
          let mb = getDevMailbox() in
            if List.isEmpty mb
            then None
            else Some <| List.exactlyOne mb
        return
          { OldEvent = oldEvent
            Participant = participant
            NewEvent = newEvent
            Message = devEmail
          }
      }

    testTask "Changing non-triggering fields doesn't send any emails" {
      let! data = init id (fun ev ->
        { ev with
            Description = Generator.generateRandomString()
            Shortname = Some <| Generator.generateRandomString()
            Title = Generator.generateRandomString()
        })
      Expect.isNone data.Message "No changes shouldn't trigger any emails"
    }

    testTask "Changing start time" {
      let! data =
        init
          id
          (fun ev ->
            { ev with
                StartDate = ev.StartDate.ToDateTime().AddHours(1).ToDateTimeCustom()
                EndDate = ev.EndDate.ToDateTime().AddHours(1).ToDateTimeCustom() })
      let actual = data.Message.Value.Email.Message
      Expect.stringContains
        actual
        $" endret tidspunkt fra "
        "Changing start time didn't trigger expected message"
    }

    testTask "Changing end time" {
      let! data =
        init
          id
          (fun ev -> { ev with EndDate = ev.EndDate.ToDateTime().AddHours(1).ToDateTimeCustom() })
      let actual = data.Message.Value.Email.Message
      Expect.stringContains
        actual
        $" endret tidspunkt fra "
        "Changing end time didn't trigger expected message"
    }

    testTask "Changing location" {
      let! data = init id (fun ev -> { ev with Location = ev.Location + "_NEW_LOCATION" })
      let actual = data.Message.Value.Email.Message
      Expect.stringContains
        actual
        $" endret lokasjon fra {data.OldEvent.event.Location} til {data.NewEvent.event.Location}"
        "Changing location didn't trigger expected message"
    }

    testTask "Changing location and time" {
      let! data = init id (fun ev -> { ev with
                                        Location = ev.Location + "_NEW_LOCATION"
                                        StartDate = ev.StartDate.ToDateTime().AddHours(1).ToDateTimeCustom()
                                        EndDate = ev.EndDate.ToDateTime().AddHours(1).ToDateTimeCustom() })
      let actual = data.Message.Value.Email.Message
      Expect.stringContains
        actual
        $"har endret tidspunkt og lokasjon"
        "Changing both location and time didn't state so in the message"
      Expect.stringContains
        actual
        $"- Tidspunkt er endret fra "
        "Changing both location and time didn't include time diff"
      Expect.stringContains
        actual
        $"- Lokasjon er endret fra "
        "Changing both location and time didn't include location diff"
    }

    testTask "Changing organizer uses new organizer in message" {
      let! data = init id (fun ev ->
        { ev with
            Location = ev.Location + "_NEW_LOCATION" // just to trigger email
            OrganizerName = "ORGANIZER_" + Generator.generateRandomString()
            OrganizerEmail = "ORGANIZER_EMAIL_" + Generator.generateEmail() })
      let actual = data.Message.Value.Email.Message
      Expect.stringContains
        actual
        data.NewEvent.event.OrganizerName
        "Couldn't find new organizer name"
      Expect.stringContains
        actual
        data.NewEvent.event.OrganizerEmail
        "Couldn't find new organizer email"
      Expect.isFalse
        (actual.Contains(data.OldEvent.event.OrganizerName))
        "Name of old organizer exists in email"
      Expect.isFalse
        (actual.Contains(data.OldEvent.event.OrganizerEmail))
        "Email of old organizer exists in email"
    }
  ]
