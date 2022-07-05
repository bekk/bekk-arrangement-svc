module Tests.CreateEvent

open Expecto

open TestUtils
open Api

let tests =
    testList "Create event" [
      test "Create event without token fails" {
        Expect.expectApiUnauthorized
          (fun () -> Events.create WithoutToken.request id)
          "Unauthenticated user was able to create event"
      }

      test "Create event with authorization token works" {
        Expect.expectApiSuccess
          (fun () -> Events.create UsingJwtToken.request id)
          "Authenticated couldn't create event"
      }
    ]
