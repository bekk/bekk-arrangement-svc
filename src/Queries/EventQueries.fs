module Event.Queries

open System
open Dapper.FSharp

open Tables
open Email.Types
open Event.Models
open ResultComputationExpression

// TODO: se på _res her

let createEvent employeeId (event: WriteModel)  =
    result {
        let! event = writeToDomain (Guid.NewGuid()) event (Guid.NewGuid()) false employeeId |> Task.wrap |> ignoreContext
        let dbModel = domainToDb event
        do! insert { table eventsTable
                     value dbModel
                   }
            |> Database.runInsertQuery
        return dbToDomain (dbModel, event.ParticipantQuestions.Unwrap)
    }

let private groupEventAndShortname ls =
    ls
    |> Seq.map (fun (event: DbModel, questionDbModel: ParticipantQuestionDbModel option) ->
                    ( event
                    , questionDbModel))
    |> Seq.groupBy (fun (event, _) -> event.Id)
    |> Seq.map (fun (_eventId, listOfQuestions) -> 
        let event, _ = listOfQuestions |> Seq.head
        let sortedQuestionsForEvent =
            listOfQuestions 
            |> Seq.collect (fun (_, question) -> match question with | Some q -> [ q ] | None -> []) 
            |> Seq.sortBy (fun q -> q.Id) 
            |> Seq.map (fun q -> q.Question) 
            |> List.ofSeq

        (event, sortedQuestionsForEvent)
    )

let getEvents: Handler<Event seq> =
    result {
        let! events =
            select { table eventsTable
                     leftJoin questionsTable "EventId" "Events.Id"
                     where (ge "EndDate" DateTime.Now)
                     orderBy "StartDate" Asc }
            |> Database.runOuterJoinSelectQuery<DbModel, ParticipantQuestionDbModel> 

        let groupedEvents =
            events
            |> groupEventAndShortname

        return Seq.map dbToDomain groupedEvents
    }

let getPastEvents: Handler<Event seq> =
    result {
        let! events =
            select { table eventsTable
                     leftJoin questionsTable "EventId" "Events.Id"
                     where (lt "EndDate" DateTime.Now + eq "IsCancelled" false)
                     orderBy "StartDate" Desc }
            |> Database.runOuterJoinSelectQuery<DbModel, ParticipantQuestionDbModel>
        
        let groupedEvents =
            events
            |> groupEventAndShortname

        return Seq.map dbToDomain groupedEvents
    }

let deleteEvent (id: Types.Id): Handler<unit> =
    result {
        let! _res =
            delete { table eventsTable
                     where (eq "Id" id.Unwrap)
                   }
            |> Database.runDeleteQuery
        return ()
    }

let updateEvent (newEvent: Event): Handler<unit> =
    result {
        let newEventDb = domainToDb newEvent
        let! _res =
            update { table eventsTable
                     set newEventDb
                     where (eq "Id" newEvent.Id.Unwrap)
                   }
            |> Database.runUpdateQuery
        return ()
    }

let queryEventByEventId (eventId: Types.Id): Handler<Event> =
    result {
        let! events = 
            select { table eventsTable 
                     leftJoin questionsTable "EventId" "Events.Id"
                     where (eq "Events.Id" eventId.Unwrap)
                   }
           |> Database.runOuterJoinSelectQuery<DbModel, ParticipantQuestionDbModel> 

        let event = 
            events
            |> groupEventAndShortname
            |> Seq.tryHead

        return! 
            match event with
            | Some eventWithShortname ->
                dbToDomain eventWithShortname 
                |> Ok |> Task.wrap
            | None -> 
                Error [ UserMessages.Events.eventNotFound eventId ] 
                |> Task.wrap
    }

let queryEventsOrganizedByEmail (organizerEmail: EmailAddress): Handler<Event seq> =
    result {
        let! events =
            select { table eventsTable
                     leftJoin questionsTable "EventId" "Events.Id"
                     where (eq "Email" organizerEmail.Unwrap)
                   }
           |> Database.runOuterJoinSelectQuery<DbModel, ParticipantQuestionDbModel>

        let groupedEvents =
            events
            |> groupEventAndShortname

        return Seq.map dbToDomain groupedEvents
   }

let queryEventsOrganizedByOrganizerId (organizerId: Types.EmployeeId): Handler<Event seq> =
    result {
        let! events =
            select { table eventsTable
                     leftJoin questionsTable "EventId" "Events.Id"
                     where (eq "OrganizerId" organizerId.Unwrap)
                   }
           |> Database.runOuterJoinSelectQuery<DbModel, ParticipantQuestionDbModel>

        let groupedEvents =
            events
            |> groupEventAndShortname

        return Seq.map dbToDomain groupedEvents
    }

let queryEventByShortname (shortname: string): Handler<Event> =
    result {
        let! events =
            select { table eventsTable 
                     leftJoin questionsTable "EventId" "Events.Id"
                     where (eq "Shortname" shortname)
                   }
           |> Database.runOuterJoinSelectQuery<DbModel, ParticipantQuestionDbModel>

        let event = 
            events
            |> groupEventAndShortname
            |> Seq.tryHead

        return!
            match event with
            | Some eventWithShortname ->
                dbToDomain eventWithShortname
                |> Ok
                |> Task.wrap
            | None -> 
                Error [ UserMessages.Events.eventNotFound shortname ]
                |> Task.wrap
    }
    
let shortnameExists (shortname: string): Handler<DbModel option> =
    result {
        let! events =
            select { table eventsTable 
                     where (eq "Shortname" shortname)
                   }
           |> Database.runSelectQuery<DbModel>
        let event = Seq.tryHead events

        return!
            match event with
            | Some event ->
                Some event
                |> Ok
                |> Task.wrap
            | None ->
                None
                |> Ok
                |> Task.wrap
    }

let updateShortname (eventId: Types.Id) (shortname: string): Handler<unit> =
    result {
        let! () =
            queryEventByShortname shortname
            >> Task.map (function
                            | Ok _ -> Error [ UserMessages.Events.shortnameIsInUse shortname ]
                            | Error _ -> Ok ())
        let! _res =
            update { table eventsTable
                     set {| Shortname = shortname |}
                     where (eq "Id" eventId.Unwrap)
                   }
            |> Database.runUpdateQuery

        return ()
    }
    
let deleteShortname (shortname: string): Handler<unit> =
    result {
        let! _res =
            update { table eventsTable
                     set {| Shortname = None |}
                     where (eq "shortname" shortname)
                   }
            |> Database.runUpdateQuery

        return ()
    }

let getQuestionsForEvent (eventId: Types.Id) =
    result {
        let! questions = 
            select {
                table questionsTable
                where (eq "EventId" eventId.Unwrap)
                orderBy "Id" Asc
            }
            |> Database.runSelectQuery<ParticipantQuestionDbModel>
        return questions
    }

let insertQuestions (eventId: Types.Id) questions =
    result {
        if Seq.isEmpty questions then
            return ()
        else

        do! insert { table questionsTable
                     values (questions 
                            |> List.map (fun question -> 
                                {| EventId = eventId.Unwrap; Question = question |}))
                   } 
                   |> Database.runInsertQuery
    }

let deleteAllQuestions (eventId: Types.Id) =
    result {
        let! _res = 
            delete { table questionsTable
                     where (eq "EventId" eventId.Unwrap)
                   }
                   |> Database.runDeleteQuery
        return ()
    }

let deleteLastQuestions n (eventId: Types.Id) =
    result {
        if n <= 0 then
            return ()
        else

        let! questions = getQuestionsForEvent eventId
        let lastNQuestions = questions |> Seq.rev |> Seq.truncate n |> List.ofSeq
        let! _res =
            delete { table questionsTable
                     where (isIn "Id" (lastNQuestions |> List.map (fun q -> q.Id :> obj)))
                   }
                   |> Database.runDeleteQuery
        return ()
    }