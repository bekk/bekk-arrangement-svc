namespace Tests.General

open Tests
open Xunit

[<Collection("Database collection")>]
type Queries(fixture: DatabaseFixture) =
    let authenticatedClient =
        fixture.getAuthedClient
        
        
    [<Fact>]
    member _.``Check if event is external returns true if event is external``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = true })
        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let eventId = System.Guid.Parse createdEvent.Event.Id
            let! result = Queries.isEventExternal eventId fixture.dbContext

            match result with
            | Ok isExternal -> Assert.True(isExternal)
            | Error e -> failwith $"Test failed: {e}"
        }

    [<Fact>]
    member _.``Check if event is external returns false if event is not external``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })
        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let eventId = System.Guid.Parse createdEvent.Event.Id
            let! result = Queries.isEventExternal eventId fixture.dbContext

            match result with
            | Ok isExternal -> Assert.False(isExternal)
            | Error e -> failwith $"Test failed: {e}"
        }

        
    [<Fact>]
    member _.``Querying non-existing participant gives no result``() =
        task {
            let! nonExistingParticipant = Queries.getParticipantForEvent (System.Guid.NewGuid()) "randomEmail@email.email" fixture.dbContext
            
            match nonExistingParticipant with
            | Ok optionalParticipant -> Assert.True(optionalParticipant.IsNone)
            | Error e ->  failwith $"Test failed: {e}"
        }
        
    [<Fact>]
    member _.``Querying existing participant gives result``() =
        let event =
            TestData.createEvent (fun e -> { e with IsExternal = false })
        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! createdParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id
            let! existingParticipant = Queries.getParticipantForEvent (System.Guid.Parse createdEvent.Event.Id) createdParticipant.Email fixture.dbContext
            
            match existingParticipant with
            | Ok optionalParticipant -> Assert.True(optionalParticipant.IsSome)
            | Error e -> failwith $"Test failed: {e}"
        }

