module Tests.DeleteEvent

open Expecto
open System
open System.Net

open TestUtils

// TODO: User is organizer -> 200 (Hvordan teste dette? Mitt token er alltid admin)
let tests =
    testList "Delete event" [
      test "Delete event with token should work" {
        let event = Generator.generateEvent()
        let created = postEvent event
        let deleteResponse, _ = deleteRequestAuthenticated $"/events/{created.event.id}/delete" token
        let getResponse, _ = getRequestAuthenticated $"/events/{created.event.id}" token
        Expect.equal deleteResponse.StatusCode HttpStatusCode.OK "Deleting with token should work"
        Expect.equal getResponse.StatusCode HttpStatusCode.NotFound "After the event has been deleted we cannot find it"
      }
      
      test "Delete event with edit-token only should work" {
        let event = Generator.generateEvent()
        let created = postEvent event
        let url =
            let builder = UriBuilder($"{basePath}/events/{created.event.id}/delete")
            builder.Query <- $"editToken={created.editToken}"
            builder.ToString()
        let deleteResponse, _ = deleteRequest url
        let getResponse, _ = getRequestAuthenticated $"/events/{created.event.id}" token
        Expect.equal deleteResponse.StatusCode HttpStatusCode.OK "Deleting with token should work"
        Expect.equal getResponse.StatusCode HttpStatusCode.NotFound "After the event has been deleted we cannot find it"
      }
      test "Cancel event with token should work" {
        let event = Generator.generateEvent()
        let created = postEvent event
        let deleteResponse, _ = deleteRequestAuthenticated $"/events/{created.event.id}" token
        let getResponse, event = getRequestAuthenticated $"/events/{created.event.id}" token
        let event = decodeEvent event
        Expect.equal deleteResponse.StatusCode HttpStatusCode.OK "Cancelling with token should work"
        Expect.equal getResponse.StatusCode HttpStatusCode.OK "After the event has been cancelled we can still find it"
        Expect.equal event.isCancelled true "After cancelling the event is actually cancelled"
      }
      
      test "Cancel event with edit-token only should work" {
        let event = Generator.generateEvent()
        let created = postEvent event
        let url =
            let builder = UriBuilder($"{basePath}/events/{created.event.id}")
            builder.Query <- $"editToken={created.editToken}"
            builder.ToString()
        let deleteResponse, _ = deleteRequest url
        let getResponse, event = getRequestAuthenticated $"/events/{created.event.id}" token
        let event = decodeEvent event
        Expect.equal deleteResponse.StatusCode HttpStatusCode.OK "Cancelling with token should work"
        Expect.equal getResponse.StatusCode HttpStatusCode.OK "After the event has been cancelled we can still find it"
        Expect.equal event.isCancelled true "After cancelling the event is actually cancelled"
      }
      
      test "Delete participant using admin token" {
        let event = { Generator.generateEvent() with IsExternal = true; HasWaitingList = true; MaxParticipants = Some 1 }
        let created = postEvent event
        let participant =
            Generator.generateParticipant (List.length event.ParticipantQuestions)
            |> toJson
        let email = Generator.generateEmail()
        let _, _ = postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token
        let response, _ = deleteRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token
        let _, numberOfParticipants = getRequestAuthenticated $"/events/{created.event.id}/participants/count" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Can delete participant as admin"
        Expect.equal numberOfParticipants "0" "Zero participants after deleting the only one"
      }
      
      test "Delete participant using cancellation token" {
        let event = { Generator.generateEvent() with IsExternal = true; HasWaitingList = true; MaxParticipants = Some 1 }
        let created = postEvent event
        let participant =
            Generator.generateParticipant (List.length event.ParticipantQuestions)
            |> toJson
        let email = Generator.generateEmail()
        let _, participant = postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token
        let participantModel = decodeParticipantWithCancellationToken participant
        let url =
            let builder = UriBuilder($"{basePath}/events/{created.event.id}/participants/{email}")
            builder.Query <- $"cancellationToken={participantModel.cancellationToken}"
            builder.ToString()
        let response, _ = deleteRequestWithBody participant url
        let _, numberOfParticipants = getRequestAuthenticated $"/events/{created.event.id}/participants/count" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Can delete participant as admin"
        Expect.equal numberOfParticipants "0" "Zero participants after deleting the only one"
      }
     
      test "Waitlist should be updated when deleting participant" {
        let event = { Generator.generateEvent() with IsExternal = true; HasWaitingList = true; MaxParticipants = Some 1 }
        let created = postEvent event
        let firstParticipant =
            Generator.generateParticipant (List.length event.ParticipantQuestions)
            |> toJson
        let firstEmail = Generator.generateEmail()
        let _, response =
            postRequestAuthenticatedWithBody firstParticipant $"/events/{created.event.id}/participants/{firstEmail}" token
        let decodedFirstParticipant = decodeParticipantWithCancellationToken response
        let secondParticipant =
            Generator.generateParticipant (List.length event.ParticipantQuestions)
            |> toJson
        let secondEmail = Generator.generateEmail()
        let _, _ =
            postRequestAuthenticatedWithBody secondParticipant $"/events/{created.event.id}/participants/{secondEmail}" token
        let _, spotBeforeDelete = getRequestAuthenticated $"/events/{created.event.id}/participants/{secondEmail}/waitinglist-spot" token
        Expect.equal spotBeforeDelete "1" "Second participant has first spot in waitlist"
        let url =
            let builder = UriBuilder($"{basePath}/events/{created.event.id}/participants/{firstEmail}")
            builder.Query <- $"cancellationToken={decodedFirstParticipant.cancellationToken}"
            builder.ToString()
        let response, _ = deleteRequestWithBody firstParticipant url
        let _, spotBeforeDelete = getRequestAuthenticated $"/events/{created.event.id}/participants/{secondEmail}/waitinglist-spot" token
        Expect.equal spotBeforeDelete "0" "Second participant has first spot in waitlist"
        let _, numberOfParticipants = getRequestAuthenticated $"/events/{created.event.id}/participants/count" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Can delete participant as admin"
        Expect.equal numberOfParticipants "1" "1 participants after deleting the only first"
      }
    ]