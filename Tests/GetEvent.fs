module Tests.GetEvent

open Expecto
open System
open System.Net

open TestUtils

let tests =
    testList "Get event" [
      test "Anyone can get event id by shortname" {
        let created =
          let event = { Generator.generateEvent() with Shortname = Some <| Generator.generateRandomString() }
          postEvent event
        let url =
            let builder = UriBuilder($"{basePath}/events/id")
            builder.Query <- $"shortname={created.event.shortname.Value}"
            builder.ToString()
        let response, id = getRequest url
        Expect.equal response.StatusCode HttpStatusCode.OK "Should get Id"
        Expect.equal $"\"{created.event.id}\"" id "ID created and ID fetched are the same"
      }
      
      test "External event can be seen by anyone" {
        let created =
          let event = { Generator.generateEvent() with IsExternal = true }
          postEvent event
        let response, _ = getRequest $"/events/{created.event.id}"
        Expect.equal response.StatusCode HttpStatusCode.OK "Should get Id"
      }
      
      test "Internal event cannot be seen if not authenticated" {
        let created =
          let event = { Generator.generateEvent() with IsExternal = false }
          postEvent event
        let response, _ = getRequest $"/events/{created.event.id}"
        Expect.equal response.StatusCode HttpStatusCode.Forbidden "Event cannot be accessed"
      }
      
      test "Internal event can be seen if authenticated" {
        let created =
          let event = { Generator.generateEvent() with IsExternal = false }
          postEvent event
        let response, _ = getRequestAuthenticated $"/events/{created.event.id}" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Event should be found"
      }
      
      test "Unfurl event can be seen by anyone" {
        let created =
          let event = { Generator.generateEvent() with IsExternal = false }
          postEvent event
        let response, _ = getRequest $"/events/{created.event.id}/unfurl"
        Expect.equal response.StatusCode HttpStatusCode.OK "Event should be found"
      }
      
      test "Participants can be counted by anyone if event is external" {
        let created =
          let event = { Generator.generateEvent() with IsExternal = true }
          postEvent event
        let response, _ = getRequest $"/events/{created.event.id}/participants/count"
        Expect.equal response.StatusCode HttpStatusCode.OK "Should get Id"
      }
      
      test "Participants cannot be counted by externals if event is internal" {
        let created =
          let event = { Generator.generateEvent() with IsExternal = false }
          postEvent event
        let response, c = getRequest $"/events/{created.event.id}/participants/count"
      
        Expect.equal response.StatusCode HttpStatusCode.Forbidden "Event cannot be read if not authenticated"
      }
      
      test "Participants can be counted by by authorized user if event is internal" {
        let created =
          let event = { Generator.generateEvent() with IsExternal = false }
          postEvent event
        let response, _ = getRequestAuthenticated $"/events/{created.event.id}/participants/count" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Event should be found"
      }
      
      test "Counting participants returns correct number" {
        let event = { Generator.generateEvent() with IsExternal = false }
        let created = postEvent event
        [0..4]
        |> List.map (fun _ -> toJson <| Generator.generateParticipant (List.length event.ParticipantQuestions))
        |> List.iter (fun p ->
            let email = Generator.generateEmail ()
            postRequestAuthenticatedWithBody p $"/events/{created.event.id}/participants/{email}" token |> ignore
          )
        let response, result = getRequestAuthenticated $"/events/{created.event.id}/participants/count" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Event should be found"
        Expect.equal result "5" "Event should have 5 participants"
      }
      
      test "Can get waitlist spot if event is external" {
        let event = { Generator.generateEvent() with IsExternal = true; IsHidden = false }
        let created = postEvent event
        let participant = toJson <| Generator.generateParticipant (List.length event.ParticipantQuestions)
        let email = Generator.generateEmail ()
        postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token |> ignore
        let response, _ = getRequest $"/events/{created.event.id}/participants/{email}/waitinglist-spot"
        Expect.equal response.StatusCode HttpStatusCode.OK "External can find waitlist spot when external"
      }
      
      test "Can get waitlist spot if event is internal" {
        let event = { Generator.generateEvent() with IsExternal = false; IsHidden = false; HasWaitingList = true }
        let created = postEvent event
        let participant = toJson <| Generator.generateParticipant (List.length event.ParticipantQuestions)
        let email = Generator.generateEmail ()
        postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token |> ignore
        let response, _ = getRequestAuthenticated $"/events/{created.event.id}/participants/{email}/waitinglist-spot" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Bekker can find waitlist spot when internal event"
      }
      
      test "Find correct waitlist spot" {
        let event = { Generator.generateEvent() with IsExternal = false; IsHidden = false; MaxParticipants = Some 0; HasWaitingList = true }
        let created = postEvent event
        let participantEmails = 
          [0..4]
          |> List.map (fun _ -> toJson <| Generator.generateParticipant (List.length event.ParticipantQuestions))
          |> List.map (fun p ->
              let email = Generator.generateEmail ()
              postRequestAuthenticatedWithBody p $"/events/{created.event.id}/participants/{email}" token |> ignore
              email
            )
        let lastEmail =
          participantEmails
          |> List.rev
          |> List.head
        let response, result = getRequestAuthenticated $"/events/{created.event.id}/participants/{lastEmail}/waitinglist-spot" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Event should be found"
        Expect.equal result "5" "Event should have 5 participants"
      }
      
      // TODO: User is organizer -> 200 (Hvordan teste dette? Mitt token er alltid admin)
      test "Export event CSV with token should work" {
        let event = { Generator.generateEvent() with IsExternal = false; MaxParticipants = None }
        let created = postEvent event
        [0..4]
        |> List.map (fun _ -> toJson <| Generator.generateParticipant (List.length event.ParticipantQuestions))
        |> List.iter (fun p ->
            let email = Generator.generateEmail ()
            postRequestAuthenticatedWithBody p $"/events/{created.event.id}/participants/{email}" token |> ignore)
        let response, _ = getRequestAuthenticated $"/events/{created.event.id}/participants/export" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Event should be found"
      }
      
      test "Export event csv with edit-token only should work" {
        let event = { Generator.generateEvent() with IsExternal = false; MaxParticipants = None }
        let created = postEvent event
        [0..4]
        |> List.map (fun _ -> toJson <| Generator.generateParticipant (List.length event.ParticipantQuestions))
        |> List.iter (fun p ->
            let email = Generator.generateEmail ()
            postRequestAuthenticatedWithBody p $"/events/{created.event.id}/participants/{email}" token |> ignore )
        let url =
            let builder = UriBuilder($"{basePath}/events/{created.event.id}/participants/export")
            builder.Query <- $"editToken={created.editToken}"
            builder.ToString()
        let response, _ = getRequest url
        Expect.equal response.StatusCode HttpStatusCode.OK "Event should be found"
      }
      
      test "Externals cannot get future events" {
        let response, _ = getRequest "/events"
        Expect.equal response.StatusCode HttpStatusCode.Unauthorized "Externals cannot get future events"
      }
      
      test "Internals can get future events" {
        let response, _ = getRequestAuthenticated "/events" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Internals can get future events"
      }
      
      test "Externals cannot get past events" {
        let response, _ = getRequest "/events/previous"
        Expect.equal response.StatusCode HttpStatusCode.Unauthorized "Externals cannot get past events"
      }
      
      test "Internals can get past events" {
        let response, _ = getRequestAuthenticated "/events/previous" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Internals can get past events"
      }
      
      test "Externals cannot get forside events" {
        let email = Generator.generateEmail () 
        let response, _ = getRequest $"/events/forside/{email}"
        Expect.equal response.StatusCode HttpStatusCode.Unauthorized "Externals cannot get forside events"
      }
      
      test "Internals can get forside events" {
        let email = Generator.generateEmail () 
        let response, _ = getRequestAuthenticated $"/events/forside/{email}" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Internals can get forside events"
      }
      
      test "Externals cannot get events organized by id" {
        let response, _ = getRequest "/events/organizer/0"
        Expect.equal response.StatusCode HttpStatusCode.Unauthorized "Externals cannot get events organized by id"
      }
      
      test "Internals can get events organized by id" {
        let response, _ = getRequestAuthenticated "/events/organizer/0" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Internals can get events organized by id"
      }
      
      test "Externals cannot get events and participations" {
        let response, _ = getRequest "/events-and-participations/0"
        Expect.equal response.StatusCode HttpStatusCode.Unauthorized "Externals cannot get events and participations" 
      }
      
      test "Internals can get events and participations" {
        let response, _ = getRequestAuthenticated "/events-and-participations/0" token
        Expect.equal response.StatusCode HttpStatusCode.OK "Internals can get events and participations" 
      }
      
      test "Externals cannot participations for event" {
        let event = { Generator.generateEvent() with IsExternal = false; MaxParticipants = None }
        let created = postEvent event
        let response, _ = getRequest $"/events/{created}/participants"
        Expect.equal response.StatusCode HttpStatusCode.Unauthorized "Externals cannot get participations for event"
      }
      
      test "Internals can participations for event" {
        let event = { Generator.generateEvent() with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true }
        let created = postEvent event
        [0..6]
        |> List.map (fun _ -> toJson <| Generator.generateParticipant (List.length event.ParticipantQuestions))
        |> List.iter (fun p ->
            let email = Generator.generateEmail ()
            postRequestAuthenticatedWithBody p $"/events/{created.event.id}/participants/{email}" token |> ignore
          )
        let response, content = getRequestAuthenticated $"/events/{created.event.id}/participants" token
        let content = decodeAttendeesAndWaitlist content
        Expect.equal (List.length content.attendees) 3 "Got 3 attendees"
        Expect.equal (List.length content.waitingList) 4 "Got 4 on waitlist"
        Expect.equal response.StatusCode HttpStatusCode.OK "internals cannot participations for event"
      }
      
      test "Externals cannot get participations for participant" {
        let email = Generator.generateEmail()
        let response, _ = getRequest $"/participants/{email}/events"
        Expect.equal response.StatusCode HttpStatusCode.Unauthorized "Externals cannot get participations for participant"
      }
      
      test "Internals can get participations for participant" {
        let events =
          [0..4]
          |> List.map (fun _ -> { Generator.generateEvent() with IsExternal = false; MaxParticipants = Some 3; HasWaitingList = true; ParticipantQuestions = [] } )
        let created =
           events
           |> List.map postEvent
           |> List.map (fun x -> x.event.id)
        let participant =
            Generator.generateParticipant 0
            |> toJson
        let email = Generator.generateEmail()
        List.iter (fun id -> postRequestAuthenticatedWithBody participant $"/events/{id}/participants/{email}" token |> ignore) created
        let response, content = getRequestAuthenticated  $"/participants/{email}/events" token
        let content = decodeParticipant content
        Expect.equal response.StatusCode HttpStatusCode.OK "Internals can get participations for participant"
        Expect.equal (List.length content) 5 "Participant has 5 participations"
      }
    ]