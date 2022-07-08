module Tests.CreateEvent

open Expecto

open TestUtils
open Api

let tests =
    testList "Create event" [
      testTask "Create event without token fails" {
        do! Expect.expectApiUnauthorized
                (fun () -> Events.create WithoutToken.request id)
                "Unauthenticated user was able to create event"
      }

      testTask "Create event with authorization token works" {
        do! Expect.expectApiSuccess
                (fun () -> Events.create UsingJwtToken.request id)
                "Authenticated couldn't create event"
      }
    ]
