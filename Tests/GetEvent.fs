module Tests.GetEvent

open Expecto

open TestUtils
open Api
open Tests.Api

let tests =
    testList "Get event" [
      testTask "Anyone can get event id by shortname" {
        let! event = TestData.createEvent (fun ev -> { ev with Shortname = Some <| Generator.generateRandomString() })
        do! Expect.expectApiMessage
              (fun () -> UsingShortName.request event.shortName.Value "/events/id" None |> Api.get |> Task.map decode<string>)
              event.id
              "ID created and ID fetched should equal"
      }

      testTask "External event can be seen by anyone" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = true })
        do! Expect.expectApiSuccess
              (fun () -> Events.get WithoutToken.request event.id)
              "Unauthenticated user not able to see external event"
      }

      testTask "Internal event cannot be seen if not authenticated" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        do! Expect.expectApiError
              (fun () -> Events.get WithoutToken.request event.id)
              "Du må enten være innlogget eller arrangementet må være eksternt for at du skal få tilgang"
              "Unexpected error"
      }

      testTask "Internal event can be seen if authenticated" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        do! Expect.expectApiSuccess
              (fun () -> Events.get UsingJwtToken.request event.id)
              "Authenticated users couldn't see internal event"
      }

      testTask "Unfurl event can be seen by anyone" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        do! Expect.expectApiSuccess
              (fun () -> WithoutToken.request $"/events/{event.id}/unfurl" None |> Api.get)
              "Unauthenticated should be able unfurl internal events"
      }

      testTask "Participants can be counted by anyone if event is external" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = true })
        do! Expect.expectApiSuccess
              (fun () -> WithoutToken.request $"/events/{event.id}/participants/count" None |> Api.get)
              "Unauthenticated user couldn't count participants for internal event"
      }

      testTask "Participants cannot be counted by externals if event is internal" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        do! Expect.expectApiForbidden
              (fun () -> WithoutToken.request $"/events/{event.id}/participants/count" None |> Api.get)
              "Unexpected error"
      }

      testTask "Participants can be counted by authorized user if event is internal" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        do! Expect.expectApiSuccess
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
              "Authorized user unable to count participants"
      }

      testTask "Counting participants returns correct number" {
        let! event = TestData.createEvent (fun ev -> { ev with MaxParticipants = None; IsExternal = false })
        for _ in 0..4 do
          do! justEffect <| Participant.create UsingJwtToken.request event id id
        do! Expect.expectApiMessage
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
              "5"
              "Event should have 5 participants"
      }

      testTask "Can get waitlist spot if event is external" {
        let! event = TestData.createEvent (fun ev -> { ev with MaxParticipants = None; IsExternal = true; IsHidden = true })
        let! participant = justContent <| Participant.create UsingJwtToken.request event id id
        do! Expect.expectApiSuccess
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{participant.email}/waitinglist-spot" None |> Api.get)
              "Authenticated unable to get waiting list spot"
      }

      testTask "Can get waitlist spot if event is internal" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false; IsHidden = false; HasWaitingList = true })
        let! participant = justContent <| Participant.create UsingJwtToken.request event id id
        do! Expect.expectApiSuccess
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{participant.email}/waitinglist-spot" None |> Api.get)
              "Authenticated user cannot get waiting-spot for internal event"
      }

      // FIXME: Doesn't compile!
      (*
      testTask "Find correct waitlist spot" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false; IsHidden = false; MaxParticipants = Some 0; HasWaitingList = true })
        let! participants =
          Seq.init 5 (fun _ -> justContent <| Participant.create UsingJwtToken.request event id id)
          |> Task.sequence
        let! lastParticipant = participants |> Seq.last
        do! Expect.expectApiMessage
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{lastParticipant.email}/waitinglist-spot" None |> Api.get)
              "5"
              "Event should have 5 participants"
      }
      *)

      // TODO: User is organizer -> 200 (Hvordan teste dette? Mitt token er alltid admin)
      testTask "Export event CSV with token should work" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = None })
        for _ in 0..4 do
          do! justEffect <| Participant.create UsingJwtToken.request event id id
        do! Expect.expectApiSuccess
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/export" None |> Api.get)
              "Authenticated user cannot export event CSV"
      }

      testTask "Export event csv with edit-token only should work" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = None })
        for _ in 0..4 do
          do! justEffect <| Participant.create UsingJwtToken.request event id id
        do! Expect.expectApiSuccess
              (fun () -> UsingEditToken.request event.editToken $"{basePath}/events/{event.id}/participants/export" None |> Api.get)
              "User cannot export event CSV using edit-token"
      }

      testTask "Externals cannot get future events" {
        do! Expect.expectApiUnauthorized
              (fun () -> Events.future WithoutToken.request)
              "Unexpected error"
      }

      testTask "Internals can get future events" {
        do! Expect.expectApiSuccess
              (fun () -> Events.future UsingJwtToken.request)
              "Unexpected error"
      }

      testTask "Externals cannot get past events" {
        do! Expect.expectApiUnauthorized
              (fun () -> Events.previous WithoutToken.request)
              "Unexpected error"
      }

      testTask "Internals can get past events" {
        do! Expect.expectApiSuccess
              (fun () -> Events.previous UsingJwtToken.request)
              "Unexpected error"
      }

      testTask "Externals cannot get forside events" {
        let email = Generator.generateEmail ()
        do! Expect.expectApiUnauthorized
              (fun () -> Events.forside WithoutToken.request email)
              "Unexpected error"
      }

      testTask "Internals can get forside events" {
        let email = Generator.generateEmail ()
        do! Expect.expectApiSuccess
              (fun () -> Events.forside UsingJwtToken.request email)
              "Authenticated user cannot get forside events"
      }

      testTask "Externals cannot get events organized by id" {
        do! Expect.expectApiUnauthorized
              (fun () -> WithoutToken.request "/events/organizer/0" None |> Api.get)
              "Unexpected error"
      }

      testTask "Internals can get events organized by id" {
        do! Expect.expectApiSuccess
              (fun () -> UsingJwtToken.request "/events/organizer/0" None |> Api.get)
              "Authenticated user couldn't get organized by id"
      }

      testTask "Externals cannot get events and participations" {
        do! Expect.expectApiUnauthorized
              (fun () -> WithoutToken.request "/events-and-participations/0" None |> Api.get)
              "External couldn't get events and participations"
      }

      testTask "Internals can get events and participations" {
        do! Expect.expectApiSuccess
              (fun () -> UsingJwtToken.request "/events-and-participations/0" None |> Api.get)
              "Authenticated user couldn't get events and participations"
      }

      testTask "Externals cannot get participants for event" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = None })
        do! Expect.expectApiUnauthorized
              (fun () -> WithoutToken.request $"/events/{event.id}/participants" None |> Api.get)
              "Unexpected error"
      }

      // FIXME: Doesn't compile!
      (*
      testTask "Internals can get participants for event" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true })
        do!
          List.init 7 (fun _ -> justEffect <| Participant.create UsingJwtToken.request event id id)
          |> Task.whenAll
        let! result = UsingJwtToken.request $"/events/{event.id}/participants" None |> Api.get |> Task.map decodeAttendeesAndWaitlist |> justContent
        Expect.equal (List.length result.attendees) 3 "Got 3 attendees"
        Expect.equal (List.length result.waitingList) 4 "Got 4 on waitlist"
      }
      *)

      testTask "Externals cannot get participations for participant" {
        let email = Generator.generateEmail()
        do! Expect.expectApiUnauthorized
              (fun () -> WithoutToken.request $"/participants/{email}/events" None |> Api.get)
              "Expected unauthorized"
      }

      testTask "Internals can get participations for participant" {
        let! events =
          List.init 5 (fun _ -> TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true; ParticipantQuestions = [] }))
          |> Task.sequence
        let participant = Generator.generateParticipant 0
        let email = Generator.generateEmail()
        do!
          events
          |> Seq.map (fun ev -> justEffect <| Participant.create UsingJwtToken.request ev (always participant) (always email))
          |> Task.whenAll
        let! numParticipants =
          UsingJwtToken.request $"/participants/{email}/events" None
          |> Api.get
          |> Task.map decodeParticipantEvents
          |> justContent
        Expect.equal (List.length numParticipants) 5 "Participant has 5 participations"
      }
    ]
