module Tests.General

open Expecto

open TestUtils
open Api

let tests =
  testList "General" [
      test "Health endpoint works" {
          Expect.expectApiMessage
              (fun () -> Health.get WithoutToken.request)
              "Health check: dette gikk fint"
              "Health check failed for unauthenticated user"

          Expect.expectApiMessage
              (fun () -> Health.get UsingJwtToken.request)
              "Health check: dette gikk fint"
              "Health check failed for authenticated user"
      }
  ]
