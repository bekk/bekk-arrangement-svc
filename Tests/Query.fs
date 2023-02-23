namespace Tests.General

open Models
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
    member _.``Check if admin can edit event returns true when isAdmin = true``() =
        let event = TestData.createEvent id
        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let eventId = System.Guid.Parse createdEvent.Event.Id
            let! result = Queries.canEditEvent eventId true None "" fixture.dbContext

            match result with
            | Ok canEdit -> Assert.True(canEdit)
            | Error e -> failwith $"Test failed: {e}"
        }
    [<Fact>]
    member _.``Check if organizer can edit event returns true when OrganizerId matches employeeId``() =
        let event = TestData.createEvent id
        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let eventId = System.Guid.Parse createdEvent.Event.Id
            let employeeId = createdEvent.Event.OrganizerId
            let! result = Queries.canEditEvent eventId false (Some employeeId) System.Guid.Empty fixture.dbContext
    
            match result with
            | Ok canEdit -> Assert.True(canEdit)
            | Error e -> failwith $"Test failed: {e}"
        }
        
    [<Fact>]
    member _.``Check if edit token can edit event returns true when EditToken matches``() =
        let event =
            TestData.createEvent id
        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let eventId = System.Guid.Parse createdEvent.Event.Id
            let editToken = createdEvent.EditToken
            let! result = Queries.canEditEvent eventId false None editToken fixture.dbContext
    
            match result with
            | Ok canEdit -> Assert.True(canEdit)
            | Error e -> failwith $"Test failed: {e}"
        }
    
    [<Fact>]
    member _.``Check if non-admin, non-organizer, and non-token holder cannot edit event``() =
        let event = TestData.createEvent id
        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let eventId = System.Guid.Parse createdEvent.Event.Id
            let! result = Queries.canEditEvent eventId false None System.Guid.Empty fixture.dbContext
    
            match result with
            | Ok canEdit -> Assert.False(canEdit)
            | Error e -> failwith $"Test failed: {e}"
        }
                
    [<Fact>]
    member _.``Getting events from forside. Has correct user specific info when is waitlisted``() =
        let event = TestData.createEvent (fun e -> { e with IsExternal = false; HasWaitingList = true; MaxParticipants = Some 0 })
        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! _ = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id
            let! _ = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id
            let! createdParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id
            let! getEventsForForside = Queries.getEventsForForside createdParticipant.Email fixture.dbContext
            
            match getEventsForForside with
            | Error e -> failwith $"Test failed: {e}"
            | Ok events ->
                let returnedEvent =
                    events
                    |> Seq.tryFind (fun (x: ForsideEvent) -> x.Id.ToString() = createdEvent.Event.Id)
                match returnedEvent with
                | None -> failwith "Test failed: Could not find event"
                | Some event ->
                    Assert.True(event.HasWaitingList)
                    Assert.True(event.IsParticipating)
                    Assert.False(event.HasRoom)
                    Assert.True(event.IsWaitlisted)
                    Assert.Equal(3, event.PositionInWaitlist)
        }
        
    [<Fact>]
    member _.``Getting events from forside. Has correct user specific info when is not waitlisted``() =
        let event = TestData.createEvent (fun e -> { e with IsExternal = false; MaxParticipants = None })
        task {
            let! createdEvent = Helpers.createEventAndGet authenticatedClient event
            let! createdParticipant = Helpers.createParticipantAndGet authenticatedClient createdEvent.Event.Id
            let! getEventsForForside = Queries.getEventsForForside createdParticipant.Email fixture.dbContext
            
            match getEventsForForside with
            | Error e -> failwith $"Test failed: {e}"
            | Ok events ->
                let returnedEvent =
                    events
                    |> Seq.tryFind (fun (x: ForsideEvent) -> x.Id.ToString() = createdEvent.Event.Id)
                match returnedEvent with
                | None -> failwith "Test failed: Could not find event"
                | Some event ->
                    Assert.True(event.HasWaitingList)
                    Assert.True(event.IsParticipating)
                    Assert.True(event.HasRoom)
                    Assert.False(event.IsWaitlisted)
                    Assert.Equal(0, event.PositionInWaitlist)
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

