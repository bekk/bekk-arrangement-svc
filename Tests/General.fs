module Tests.General

open Expecto

open TestUtils
open Api

let tests =
  testList "General" [
      testTask "Health endpoint works" {
          do! Expect.expectApiMessage
                  (fun () -> Health.get WithoutToken.request)
                  "Health check: dette gikk fint"
                  "Health check failed for unauthenticated user"

          do! Expect.expectApiMessage
                  (fun () -> Health.get UsingJwtToken.request)
                  "Health check: dette gikk fint"
                  "Health check failed for authenticated user"
      }
  ]
