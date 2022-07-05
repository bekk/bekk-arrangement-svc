module Tests.DeleteEvent

open Expecto

open TestUtils
open Api

// TODO: User is organizer -> 200 (Hvordan teste dette? Mitt token er alltid admin)
let tests =
    testList "Delete event" [
      test "Delete event with token should work" {
        let event = TestData.createEvent id
        justEffect <| Events.delete UsingJwtToken.request event
        Expect.expectApiNotfound
            (fun () -> Events.get UsingJwtToken.request event.id)
            "Get not returning NotFound for event deleted with jwt token"
      }

      test "Delete event with edit-token only should work" {
        let event = TestData.createEvent id
        justEffect <| Events.delete (UsingEditToken.request event.editToken) event
        Expect.expectApiNotfound
          (fun () -> Events.get UsingJwtToken.request event.id)
            "Get not returning NotFound for event deleted with edit-token"
      }

      test "Cancel event with token should work" {
        let event = TestData.createEvent id
        justEffect <| Events.cancel UsingJwtToken.request event
        let fetched = justContent <| Events.get UsingJwtToken.request event.id
        Expect.isTrue fetched.isCancelled "Cancel using jwt token didn't mark the event as cancelled"
      }

      test "Cancel event with edit-token only should work" {
        let event = TestData.createEvent id
        justEffect <| Events.cancel (UsingEditToken.request event.editToken) event
        let fetched = justContent <| Events.get UsingJwtToken.request event.id
        Expect.isTrue fetched.isCancelled "Cancel using edit-token didn't mark the event as cancelled"
      }

      test "Delete participant using admin token" {
        let event = TestData.createEvent (fun ev -> { ev with  IsExternal = true; HasWaitingList = true; MaxParticipants = Some 1})
        let participant = justContent <| Participant.create UsingJwtToken.request event id id
        justEffect <| Participant.delete UsingJwtToken.request participant
        Expect.expectApiMessage
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
          "0"
          "Expected 0 participants"
      }

      test "Delete participant using cancellation token" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = true; HasWaitingList = true; MaxParticipants = Some 1 })
        let participant = justContent <| Participant.create UsingJwtToken.request event id id
        justEffect <| Participant.delete (UsingCancellationToken.request participant.cancellationToken) participant
        Expect.expectApiMessage
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
          "0"
          "Expected 0 participants"
      }

      test "Waitlist should be updated when deleting participant" {
        let event = TestData.createEvent (fun ev -> { ev with IsExternal = true; HasWaitingList = true; MaxParticipants = Some 1 })
        let firstParticipant = justContent <| Participant.create UsingJwtToken.request event id id
        let secondParticipant = justContent <| Participant.create UsingJwtToken.request event id id

        Expect.expectApiMessage
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{secondParticipant.email}/waitinglist-spot" None |> Api.get)
          "1"
          "Second participant should have first spot in waitlist"

        justEffect <| Participant.delete UsingJwtToken.request firstParticipant
        Expect.expectApiMessage
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{secondParticipant.email}/waitinglist-spot" None |> Api.get)
          "0"
          "Second participant should have first spot in waitlist"

        Expect.expectApiMessage
          (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
          "1"
          "1 participants after deleting the only first"
      }
    ]
