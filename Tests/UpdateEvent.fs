module Tests.UpdateEvent

open Expecto
open System
open System.Net

open TestUtils

// TODO: User is organizer -> 200 (Hvordan teste dette? Mitt token er alltid admin)
let tests =
    testList "Update event" [
      test "Update event with token should work" {
        let event = Generator.generateEvent()
        let created = postEvent event
        let updatedEvent = { event with Title = "This is a new title!" } |> toJson
        let response, _ = putRequestAuthenticatedWithBody updatedEvent $"/events/{created.event.id}" token
        Expect.equal response.StatusCode HttpStatusCode.OK "A request with token should work"
      }

      test "Update event with edit-token only should work" {
        let event = Generator.generateEvent()
        let created = postEvent event
        let updatedEvent = { event with Title = "This is a new title!" } |> toJson
        let url =
            let builder = UriBuilder($"{basePath}/events/{created.event.id}")
            builder.Query <- $"editToken={created.editToken}"
            builder.ToString()
        let response, _ = putRequestWithBody updatedEvent url
        Expect.equal response.StatusCode HttpStatusCode.OK "A request with edit-token only should work"
      }
    ]
