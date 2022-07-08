module Tests.DeleteEvent

open Expecto

open TestUtils
open Api

// TODO: User is organizer -> 200 (Hvordan teste dette? Mitt token er alltid admin)
let tests =
    testList "Delete event" [
      testTask "Delete event with token should work" {
        let! event = TestData.createEvent id
        do! justEffect <| Events.delete UsingJwtToken.request event
        do! Expect.expectApiNotfound
              (fun () -> Events.get UsingJwtToken.request event.id)
              "Get not returning NotFound for event deleted with jwt token"
      }

      testTask "Delete event with edit-token only should work" {
        let! event = TestData.createEvent id
        do! justEffect <| Events.delete (UsingEditToken.request event.editToken) event
        do! Expect.expectApiNotfound
              (fun () -> Events.get UsingJwtToken.request event.id)
                "Get not returning NotFound for event deleted with edit-token"
      }

      testTask "Cancel event with token should work" {
        let! event = TestData.createEvent id
        do! justEffect <| Events.cancel UsingJwtToken.request event
        let! fetched = justContent <| Events.get UsingJwtToken.request event.id
        // FIXME: Doesn't compile
        //Expect.isTrue fetched.isCancelled "Cancel using jwt token didn't mark the event as cancelled"
        return ()
      }

      testTask "Cancel event with edit-token only should work" {
        let! event = TestData.createEvent id
        do! justEffect <| Events.cancel (UsingEditToken.request event.editToken) event
        let! fetched = justContent <| Events.get UsingJwtToken.request event.id
        // FIXME: Doesn't compile
        //Expect.isTrue fetched.isCancelled "Cancel using edit-token didn't mark the event as cancelled"
        return ()
      }

      testTask "Delete participant using admin token" {
        let! event = TestData.createEvent (fun ev -> { ev with  IsExternal = true; HasWaitingList = true; MaxParticipants = Some 1})
        let! participant = justContent <| Participant.create UsingJwtToken.request event id id
        do! justEffect <| Participant.delete UsingJwtToken.request participant
        do! Expect.expectApiMessage
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
              "0"
              "Expected 0 participants"
      }

      testTask "Delete participant using cancellation token" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = true; HasWaitingList = true; MaxParticipants = Some 1 })
        let! participant = justContent <| Participant.create UsingJwtToken.request event id id
        do! justEffect <| Participant.delete (UsingCancellationToken.request participant.cancellationToken) participant
        do! Expect.expectApiMessage
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
              "0"
              "Expected 0 participants"
      }

      testTask "Waitlist should be updated when deleting participant" {
        let! event = TestData.createEvent (fun ev -> { ev with IsExternal = true; HasWaitingList = true; MaxParticipants = Some 1 })
        let! firstParticipant = justContent <| Participant.create UsingJwtToken.request event id id
        let! secondParticipant = justContent <| Participant.create UsingJwtToken.request event id id

        do! Expect.expectApiMessage
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{secondParticipant.email}/waitinglist-spot" None |> Api.get)
              "1"
              "Second participant should have first spot in waitlist"

        do! justEffect <| Participant.delete UsingJwtToken.request firstParticipant
        do! Expect.expectApiMessage
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/{secondParticipant.email}/waitinglist-spot" None |> Api.get)
              "0"
              "Second participant should have first spot in waitlist"

        do! Expect.expectApiMessage
              (fun () -> UsingJwtToken.request $"/events/{event.id}/participants/count" None |> Api.get)
              "1"
              "1 participants after deleting the only first"
      }
    ]
