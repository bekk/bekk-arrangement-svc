module Tests.General

open Expecto
open System.Net

open TestUtils

let tests =
  testList "General" [
      test "Health endpoint works" {
          let response, content = getRequest "/health"
          Expect.equal response.StatusCode HttpStatusCode.OK "Health check reports 200 OK"
          Expect.equal content "\"Health check: dette gikk fint\"" "Health check works"
      }
  ]
  