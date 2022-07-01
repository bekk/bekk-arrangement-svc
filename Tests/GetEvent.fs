module Tests.GetEvent

open Expecto

open TestUtils
open Api

let tests =
    testList "Get event" [
      test "Anyone can get event id by shortname" {
        let event = TestData.createEvent (fun ev -> { ev with Shortname = Some <| Generator.generateRandomString() })
        Expect.expectApiMessage
          (fun () -> UsingShortName.request event.shortName.Value "/events/id" None |> Api.get |> decode<string>)
          event.id
          "ID created and ID fetched should equal"
      }

      test "External event can be seen by anyone" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = true })
        Expect.expectApiSuccess
          (fun () -> Events.get WithoutToken.request event.id)
          "Unauthenticated user not able to see external event"
      }

      test "Internal event cannot be seen if not authenticated" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        Expect.expectApiError
          (fun () -> Events.get WithoutToken.request event.id)
          "Du må enten være innlogget eller arrangementet må være eksternt for at du skal få tilgang"
          "Unexpected error"
      }

      test "Internal event can be seen if authenticated" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        Expect.expectApiSuccess
          (fun () -> Events.get UsingJwtToken.request event.id)
          "Authenticated users couldn't see internal event"
      }

      test "Unfurl event can be seen by anyone" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        Expect.expectApiSuccess
          (fun () -> WithoutToken.request $"/events/{event.id}/unfurl" None |> Api.get)
          "Unauthenticated should be able unfurl internal events"
      }

      test "Participants can be counted by anyone if event is external" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = true })
        Expect.expectApiSuccess
          (fun () -> WithoutToken.request $"/events/{event.id}/participants/count" None |> Api.get)
          "Unauthenticated user couldn't count participants for internal event"
      }

      test "Participants cannot be counted by externals if event is internal" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        Expect.expectApiForbidden
          (fun () -> WithoutToken.request $"/events/{event.id}/participants/count" None |> Api.get)
          "Unexpected error"
      }

      test "Participants can be counted by authorized user if event is internal" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        Expect.expectApiSuccess
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
          "Authorized user unable to count participants"
      }

      test "Counting participants returns correct number" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false })
        List.init 5 (fun _ -> justEffect <| Participant.create UsingJwtToken.request event id id) |> ignore
        Expect.expectApiMessage
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
          "5"
          "Event should have 5 participants"
      }

      test "Can get waitlist spot if event is external" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = true; IsHidden = true })
        let participant = justContent <| Participant.create UsingJwtToken.request event id id
        Expect.expectApiSuccess
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{participant.email}/waitinglist-spot" None |> Api.get)
          "Authenticated unable to get waiting list spot"
      }

      test "Can get waitlist spot if event is internal" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false; IsHidden = false; HasWaitingList = true })
        let participant = justContent <| Participant.create UsingJwtToken.request event id id
        Expect.expectApiSuccess
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{participant.email}/waitinglist-spot" None |> Api.get)
          "Authenticated user cannot get waiting-spot for internal event"
      }

      test "Find correct waitlist spot" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false; IsHidden = false; MaxParticipants = Some 0; HasWaitingList = true })
        let participants = List.init 5 (fun _ -> justContent <| Participant.create UsingJwtToken.request event id id)
        let lastParticipant = participants |> List.last
        Expect.expectApiMessage
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{lastParticipant.email}/waitinglist-spot" None |> Api.get)
          "5"
          "Event should have 5 participants"
      }

      // TODO: User is organizer -> 200 (Hvordan teste dette? Mitt token er alltid admin)
      test "Export event CSV with token should work" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = None })
        List.init 5 (fun _ -> justEffect <| Participant.create UsingJwtToken.request event id id) |> ignore
        Expect.expectApiSuccess
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/export" None |> Api.get)
          "Authenticated user cannot export event CSV"
      }

      test "Export event csv with edit-token only should work" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = None })
        List.init 5 (fun _ -> justEffect <| Participant.create UsingJwtToken.request event id id) |> ignore
        Expect.expectApiSuccess
          (fun () -> UsingEditToken.request event.editToken $"{basePath}/events/{event.id}/participants/export" None |> Api.get)
          "User cannot export event CSV using edit-token"
      }

      test "Externals cannot get future events" {
        Expect.expectApiUnauthorized
          (fun () -> Events.future WithoutToken.request)
          "Unexpected error"
      }

      test "Internals can get future events" {
        Expect.expectApiSuccess
          (fun () -> Events.future UsingJwtToken.request)
          "Unexpected error"
      }

      test "Externals cannot get past events" {
        Expect.expectApiUnauthorized
          (fun () -> Events.previous WithoutToken.request)
          "Unexpected error"
      }

      test "Internals can get past events" {
        Expect.expectApiSuccess
          (fun () -> Events.previous UsingJwtToken.request)
          "Unexpected error"
      }

      test "Externals cannot get forside events" {
        let email = Generator.generateEmail ()
        Expect.expectApiUnauthorized
          (fun () -> Events.forside WithoutToken.request email)
          "Unexpected error"
      }

      test "Internals can get forside events" {
        let email = Generator.generateEmail ()
        Expect.expectApiSuccess
          (fun () -> Events.forside UsingJwtToken.request email)
          "Authenticated user cannot get forside events"
      }

      test "Externals cannot get events organized by id" {
        Expect.expectApiUnauthorized
          (fun () -> WithoutToken.request "/events/organizer/0" None |> Api.get)
          "Unexpected error"
      }

      test "Internals can get events organized by id" {
        Expect.expectApiSuccess
          (fun () -> UsingJwtToken.request "/events/organizer/0" None |> Api.get)
          "Authenticated user couldn't get organized by id"
      }

      test "Externals cannot get events and participations" {
        Expect.expectApiUnauthorized
          (fun () -> WithoutToken.request "/events-and-participations/0" None |> Api.get)
          "External couldn't get events and participations"
      }

      test "Internals can get events and participations" {
        Expect.expectApiSuccess
          (fun () -> UsingJwtToken.request "/events-and-participations/0" None |> Api.get)
          "Authenticated user couldn't get events and participations"
      }

      test "Externals cannot get participants for event" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = None })
        Expect.expectApiUnauthorized
          (fun () -> WithoutToken.request $"/events/{event.id}/participants" None |> Api.get)
          "Unexpected error"
      }

      test "Internals can get participants for event" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true })
        List.init 7 (fun _ -> justEffect <| Participant.create UsingJwtToken.request event id id) |> ignore
        let result = UsingJwtToken.request $"/events/{event.id}/participants" None |> Api.get |> decodeAttendeesAndWaitlist |> justContent
        Expect.equal (List.length result.attendees) 3 "Got 3 attendees"
        Expect.equal (List.length result.waitingList) 4 "Got 4 on waitlist"
      }

      test "Externals cannot get participations for participant" {
        let email = Generator.generateEmail()
        Expect.expectApiUnauthorized
          (fun () -> WithoutToken.request $"/participants/{email}/events" None |> Api.get)
          "Expected unauthorized"
      }

      test "Internals can get participations for participant" {
        let events = List.init 5 (fun _ -> TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true; ParticipantQuestions = [] }))
        let participant = Generator.generateParticipant 0
        let email = Generator.generateEmail()
        events |> Seq.iter (fun ev -> justEffect <| Participant.create UsingJwtToken.request ev (always participant) (always email))
        let numParticipants =
          UsingJwtToken.request $"/participants/{email}/events" None
          |> Api.get
          |> decodeParticipantEvents
          |> justContent
          |> List.length
        Expect.equal numParticipants 5 "Participant has 5 participations"
      }
    ]
