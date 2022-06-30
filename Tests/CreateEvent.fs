module Tests.CreateEvent

open System.Net
open Expecto

open TestUtils

let tests =
    testList "Create event" [
      test "Create event without token will fail" {
        let json = Generator.generateEvent() |> toJson
        let response, _ = postRequestWithBody json "/events"
        Expect.equal response.StatusCode HttpStatusCode.Unauthorized "A request without token should fail"
      }
      
      test "Create event with token will work" {
        let json = Generator.generateEvent() |> toJson
        let response, _ = postRequestAuthenticatedWithBody json "/events" token
        Expect.equal response.StatusCode HttpStatusCode.OK "A request with token should work"
      }
    ]
