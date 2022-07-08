module Tests.UpdateEvent

open Expecto

open TestUtils
open Api

// TODO: User is organizer -> 200 (Hvordan teste dette? Mitt token er alltid admin)
let tests =
    testList "Update event" [
      testTask "Update event with token should work" {
        let! created = (justContent <| Events.create UsingJwtToken.request id) : System.Threading.Tasks.Task<CreatedEvent>
        // Note that the extra type information is necessary for the compiler
        let! (updated : CreatedEvent) = justContent <| Events.update UsingJwtToken.request created (fun (ev : EventWriteModel) -> { ev with Title = "This is a new title!" })
        let! fetched = justContent <| Events.get UsingJwtToken.request created.id
        Expect.equal fetched.title updated.event.Title "Title wasn't updated using jwt token"
      }

      testTask "Update event with edit-token only should work" {
        let! created = justContent <| Events.create UsingJwtToken.request id
        // Note that the extra type information is necessary for the compiler
        let! (updated : CreatedEvent) = justContent <| Events.update (UsingEditToken.request created.editToken) created (fun ev -> { ev with Title = Generator.generateRandomString() })
        let! fetched = justContent <| Events.get UsingJwtToken.request created.id
        Expect.equal fetched.title updated.event.Title "Title wasn't updated using edit-token"
      }
    ]
