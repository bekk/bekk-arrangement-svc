module Queries

open Dapper
open System
open System.Collections.Generic

let isEventExternal eventId (db: DatabaseContext) =
    task {
        let query =
            "
            IF EXISTS (SELECT * FROM Events
                       WHERE Id = @eventId AND isExternal = 1)
                SELECT CAST(1 AS BIT)
            ELSE
                SELECT CAST(0 AS BIT);
            "

        let parameters = {|
            EventId = eventId
        |}

        try
            let! result = db.Connection.QuerySingleAsync<bool>(query, parameters, db.Transaction)
            return Ok result
        with
        | ex ->
            return Error ex
    }

let canEditEvent eventId isAdmin (employeeId: int option) editToken (db: DatabaseContext) =
    task {
        let query =
            "
            IF EXISTS (SELECT * FROM Events
                       WHERE Id = @eventId AND (@isAdmin = 1 OR OrganizerId = @employeeId OR EditToken = @editToken))
                SELECT CAST(1 AS BIT)
            ELSE
                SELECT CAST(0 AS BIT);
            "

        let parameters = {|
            EventId = eventId
            IsAdmin = isAdmin
            EmployeeId = employeeId
            EditToken = editToken
        |}

        try
            let! result = db.Connection.QuerySingleAsync<bool>(query, parameters, db.Transaction)
            return Ok result
        with
        | ex ->
            return Error ex
    }

let getEventsForForside (email: string) (db: DatabaseContext) =
    task {
        let query =
            "
             WITH participation as (
                SELECT EventId, Email, RegistrationTime, ROW_NUMBER() OVER (
                PARTITION BY EventId ORDER BY RegistrationTime
                ) registrationSpot
                FROM Participants
                GROUP BY EventId, RegistrationTime, Email
                )
            SELECT E.Id,
                   E.Title,
                   E.Location,
                   E.StartDate,
                   E.EndDate,
                   E.StartTime,
                   E.EndTime,
                   E.OpenForRegistrationTime,
                   E.CloseRegistrationTime,
                   E.MaxParticipants,
                   E.CustomHexColor,
                   E.Shortname,
                   E.hasWaitingList,
                   E.Offices,
                   IIF(e.MaxParticipants is null, 1, IIF((SELECT COUNT(*) FROM Participants p0 WHERE p0.EventId = E.Id) < E.MaxParticipants, 1, 0)) as HasRoom,
                   IIF(myp.registrationSpot is null, 0, 1) as IsParticipating,
                   IIF(myp.RegistrationSpot > E.MaxParticipants, 1, 0) as IsWaitlisted,
                   IIF(myp.RegistrationSpot > E.MaxParticipants, (myp.registrationSpot - e.MaxParticipants), 0) AS PositionInWaitlist
            FROM Events E
                     LEFT OUTER JOIN participation PN ON E.id = PN.EventId
                     LEFT OUTER JOIN participation myp ON E.Id = myp.EventId AND myp.Email = @email
            WHERE EndDate >= @now
              AND IsCancelled = 0
              AND IsHidden = 0
            GROUP BY E.Id, E.Title, E.Location, E.StartDate, E.EndDate, E.StartTime, E.EndTime, E.OpenForRegistrationTime, E.CloseRegistrationTime, E.MaxParticipants, E.CustomHexColor, E.Shortname, E.hasWaitingList, E.Offices, myp.RegistrationSpot
            "

        let parameters = {|
            Email = email
            Now = DateTime.Now.Date
        |}

        try
            let! result = db.Connection.QueryAsync<Models.ForsideEvent>(query, parameters, db.Transaction)
            return Ok result
        with
        | ex ->
            return Error ex
    }

let getEventsSummary (db: DatabaseContext) =
    task {
        let query =
            "
            SELECT E.Id,
                   E.Title,
                   E.StartDate,
                   E.IsExternal
            FROM Events E
            WHERE EndDate >= @now
                AND IsCancelled = 0
                AND IsHidden = 0
            "

        let parameters = {|
            Now = DateTime.Now.Date
        |}

        try
            let! result = db.Connection.QueryAsync<Models.EventSummary>(query, parameters, db.Transaction)
            return Ok result
        with
        | ex ->
            return Error ex
    }

let private getEventAndParticipantQuestions query numParticipantsQuery (parameters: obj) (db: DatabaseContext) =
    task {
        try
            let! rows =
                db.Connection.QueryAsync(
                    query,
                    (fun (event: Models.Event) (question: Models.ParticipantQuestion) ->
                        let question =  question :> obj |> Option.ofObj |> Option.map (fun x -> x :?> Models.ParticipantQuestion)
                        (event, question)
                    ),
                    parameters,
                    transaction = db.Transaction)

            let! count = db.Connection.QueryAsync<{| Id: Guid; NumParticipants: int |}>(numParticipantsQuery, parameters, db.Transaction)

            let groupedEvents =
                rows
                |> Seq.fold (fun state (event, question) ->
                    let numParticipants =
                        count
                        |> Seq.tryFind (fun e -> e.Id = event.Id)
                        |> Option.map (fun x -> x.NumParticipants)
                    let group =
                        // Find existing or create event if not exists
                        Map.tryFind event.Id state
                        |> Option.defaultValue ({ Event = event; NumberOfParticipants = numParticipants; Questions = [] } : Models.EventAndQuestions)
                        // Add question
                        |> fun e ->
                            question
                            |> Option.map (fun q -> { e with Questions = q :: e.Questions })
                            |> Option.defaultValue e
                    Map.add event.Id group state
                    ) Map.empty
                |> Map.values
                |> Seq.toList
            return Ok groupedEvents
        with
        | ex ->
            return Error ex
    }

// Denne querien returnerer alle events som ikke er ferdig enda
// Gjemte arrangementer vises kun dersom man arrangerer de eller deltar på de
let getFutureEvents (employeeId: int) (db: DatabaseContext) =
    task {
        let query =
            "
            WITH participation as (
                SELECT EventId, IIF(mySpot IS NULL, 0, 1) AS isPaameldt
                FROM (SELECT p.EventId AS EventId, participation.RegistrationTime AS mySpot
                      FROM (SELECT EventId, RegistrationTime FROM Participants) p
                               LEFT JOIN (SELECT EventID, RegistrationTime
                                          FROM Participants
                                          WHERE EmployeeId = @employeeId) participation ON p.EventId = participation.EventId) q
                group by eventId, mySpot
            )
            SELECT E.Id,
                   E.Title,
                   E.Description,
                   E.Location,
                   E.OrganizerName,
                   E.OrganizerEmail,
                   E.StartDate,
                   E.StartTime,
                   E.EndDate,
                   E.EndTime,
                   E.MaxParticipants,
                   E.OrganizerEmail,
                   E.OpenForRegistrationTime,
                   E.EditToken,
                   E.HasWaitingList,
                   E.IsCancelled,
                   E.IsExternal,
                   E.OrganizerId,
                   E.IsHidden,
                   E.CloseRegistrationTime,
                   E.CustomHexColor,
                   E.Shortname,
                   E.Program,
                   E.Offices,
                   P.Id,
                   P.EventId,
                   P.Question,
                   PN.isPaameldt
            FROM Events E
                     LEFT JOIN ParticipantQuestions P ON E.Id = P.EventId
                     LEFT JOIN participation PN ON PN.EventId = E.Id
            WHERE E.EndDate >= @now
            AND ((E.IsHidden = 1 AND E.OrganizerId = @employeeId) OR (E.IsHidden = 1 AND PN.isPaameldt = 1) OR E.IsHidden = 0)
            ORDER BY StartDate;
            "
        let parameters = {|
            EmployeeId = employeeId
            Now = DateTime.Now.Date
        |}

        let numParticipantsQuery =
            "
            SELECT Id, Count(Id) as NumParticipants
            FROM EVENTS
                INNER JOIN Participants P on Events.Id = P.EventId
            WHERE EndDate >= @now
            GROUP BY Id
            "

        return! getEventAndParticipantQuestions query numParticipantsQuery parameters db
    }

// Denne querien returnerer alle ferdige events
// Gjemte arrangementer vises kun dersom man arrangerer de eller deltar på de
let getPastEvents (employeeId: int) (db: DatabaseContext) =
    task {
        let query =
            "
            WITH participation as (
                SELECT EventId, IIF(mySpot IS NULL, 0, 1) AS isPaameldt
                FROM (SELECT p.EventId AS EventId, participation.RegistrationTime AS mySpot
                      FROM (SELECT EventId, RegistrationTime FROM Participants) p
                               LEFT JOIN (SELECT EventID, RegistrationTime
                                          FROM Participants
                                          WHERE EmployeeId = @employeeId) participation ON p.EventId = participation.EventId) q
                GROUP BY eventId, mySpot
            )
            SELECT E.Id,
                   E.Title,
                   E.Description,
                   E.Location,
                   E.OrganizerName,
                   E.OrganizerEmail,
                   E.StartDate,
                   E.StartTime,
                   E.EndDate,
                   E.EndTime,
                   E.MaxParticipants,
                   E.OrganizerEmail,
                   E.OpenForRegistrationTime,
                   E.EditToken,
                   E.HasWaitingList,
                   E.IsCancelled,
                   E.IsExternal,
                   E.OrganizerId,
                   E.IsHidden,
                   E.CloseRegistrationTime,
                   E.CustomHexColor,
                   E.Shortname,
                   E.Program,
                   E.Offices,
                   P.Id,
                   P.EventId,
                   P.Question,
                   PN.isPaameldt
            FROM Events E
                     LEFT JOIN ParticipantQuestions P ON E.Id = P.EventId
                     LEFT JOIN participation PN ON PN.EventId = E.Id
            WHERE E.EndDate < @now AND E.IsCancelled = 0
            AND ((E.IsHidden = 1 AND E.OrganizerId = @employeeId) OR (E.IsHidden = 1 AND PN.isPaameldt = 1) OR E.IsHidden = 0)
            ORDER BY StartDate DESC;
            "
        let parameters = {|
            EmployeeId = employeeId
            Now = DateTime.Now.Date
        |}

        let numParticipantsQuery =
            "
            SELECT Id, Count(Id) as NumParticipants
            FROM EVENTS
                INNER JOIN Participants P on Events.Id = P.EventId
            WHERE EndDate <= @now
            GROUP BY Id
            "

        return! getEventAndParticipantQuestions query numParticipantsQuery parameters db
    }

let getEventsOrganizedByEmail (email: string) (db : DatabaseContext) =
    task {
        let query =
            "
            SELECT E.Id,
                   E.Title,
                   E.Description,
                   E.Location,
                   E.OrganizerName,
                   E.OrganizerEmail,
                   E.StartDate,
                   E.StartTime,
                   E.EndDate,
                   E.EndTime,
                   E.MaxParticipants,
                   E.OrganizerEmail,
                   E.OpenForRegistrationTime,
                   E.EditToken,
                   E.HasWaitingList,
                   E.IsCancelled,
                   E.IsExternal,
                   E.OrganizerId,
                   E.IsHidden,
                   E.CloseRegistrationTime,
                   E.CustomHexColor,
                   E.Shortname,
                   E.Program,
                   E.Offices,
                   P.Id,
                   P.EventId,
                   P.Question
            FROM Events E
                     LEFT JOIN ParticipantQuestions P ON E.Id = P.EventId
            WHERE E.OrganizerEmail = @email
            ORDER BY StartDate DESC;
            "
        let parameters = {|
            Email = email
        |}

        let numParticipantsQuery =
            "
            SELECT Id, Count(Id) as NumParticipants
            FROM EVENTS
                INNER JOIN Participants P on Events.Id = P.EventId
            WHERE OrganizerEmail = @email
            GROUP BY Id
            "

        return! getEventAndParticipantQuestions query numParticipantsQuery parameters db
    }

let getEventsOrganizedById (id: int) (db: DatabaseContext) =
    task {
        let query =
            "
            SELECT E.Id,
                   E.Title,
                   E.Description,
                   E.Location,
                   E.OrganizerName,
                   E.OrganizerEmail,
                   E.StartDate,
                   E.StartTime,
                   E.EndDate,
                   E.EndTime,
                   E.MaxParticipants,
                   E.OrganizerEmail,
                   E.OpenForRegistrationTime,
                   E.EditToken,
                   E.HasWaitingList,
                   E.IsCancelled,
                   E.IsExternal,
                   E.OrganizerId,
                   E.IsHidden,
                   E.CloseRegistrationTime,
                   E.CustomHexColor,
                   E.Shortname,
                   E.Program,
                   E.Offices,
                   P.Id,
                   P.EventId,
                   P.Question
            FROM Events E
                     LEFT JOIN ParticipantQuestions P ON E.Id = P.EventId
            WHERE E.OrganizerId = @id
            ORDER BY StartDate DESC;
            "
        let parameters = {|
            Id = id
        |}

        let numParticipantsQuery =
            "
            SELECT Id, Count(Id) as NumParticipants
            FROM EVENTS
                INNER JOIN Participants P on Events.Id = P.EventId
            WHERE OrganizerId = @id
            GROUP BY Id
            "

        return! getEventAndParticipantQuestions query numParticipantsQuery parameters db
    }

let getParticipationsById (id: int) (db: DatabaseContext) =
    task {
        let query =
            "
            SELECT P.Email,
                   P.EventId,
                   P.RegistrationTime,
                   P.CancellationToken,
                   P.Name,
                   P.EmployeeId
            FROM Participants P
                     LEFT JOIN Events E ON E.Id = P.EventId AND E.OrganizerEmail = P.Email
            WHERE P.EmployeeId = @id
            ORDER BY StartDate DESC;
            "
        let parameters = {|
            Id = id
        |}

        try
            let! result = db.Connection.QueryAsync<Models.Participant>(query, parameters, db.Transaction)
            return Ok result
        with
            | ex -> return Error ex
    }

let getNumberOfParticipantsForEvent (eventId: Guid) (db: DatabaseContext) =
    task {
        let query =
            "
            SELECT Count(*)
                FROM
                Participants
                INNER JOIN Events E on E.Id = @eventId
            WHERE EventId = @eventId;
            "
        let parameters = {|
            EventId = eventId
        |}

        try
            let! result = db.Connection.QuerySingleAsync<int>(query, parameters, db.Transaction)
            return Ok result
        with
            | ex -> return Error ex
    }

// Gets event based on ID.
// Will only get an event if the user is a Bekker or the event itself is external
let getEvent (eventId: Guid) (db: DatabaseContext) =
    task {
        let query =
            "
            SELECT E.Id,
                   E.Title,
                   E.Description,
                   E.Location,
                   E.OrganizerName,
                   E.OrganizerEmail,
                   E.StartDate,
                   E.StartTime,
                   E.EndDate,
                   E.EndTime,
                   E.MaxParticipants,
                   E.OrganizerEmail,
                   E.OpenForRegistrationTime,
                   E.EditToken,
                   E.HasWaitingList,
                   E.IsCancelled,
                   E.IsExternal,
                   E.OrganizerId,
                   E.IsHidden,
                   E.CloseRegistrationTime,
                   E.CustomHexColor,
                   E.Shortname,
                   E.Program,
                   E.Offices,
                   P.Id,
                   P.EventId,
                   P.Question
            FROM Events E
                     LEFT JOIN ParticipantQuestions P ON E.Id = P.EventId
            WHERE E.Id = @eventId
            ORDER BY P.Id;
            "
        let parameters = {|
            EventId = eventId
        |}

        try
            let mutable events = []
            let mutable questions = []

            let! _ =
                db.Connection.QueryAsync(
                    query,
                    (fun (event: Models.Event) (question: Models.ParticipantQuestion) ->
                        events <- events@[event]
                        if (question :> obj <> null) then
                            questions <- questions@[question]),
                    parameters,
                    transaction = db.Transaction)

            let! numberOfParticipants = getNumberOfParticipantsForEvent eventId db

            match numberOfParticipants with
            | Ok numParticipants ->
                let result =
                    events
                    |> List.tryHead
                    |> Option.map (fun _ ->
                        let result: Models.EventAndQuestions = { Event = List.head events; NumberOfParticipants = Some numParticipants; Questions = questions}
                        result)

                return Ok result
            | Error ex ->
                return Error ex
        with
        | ex -> return Error ex
    }

let getParticipantsForEvent (eventId: Guid) (db: DatabaseContext) =
    task {
            let query =
                "
                SELECT Email,
                       EventId,
                       RegistrationTime,
                       CancellationToken,
                       Name,
                       EmployeeId
                FROM [Participants]
                WHERE EventId = @eventId
                ORDER BY RegistrationTime;
                "
            let parameters = {|
                EventId = eventId
            |}
        try
            let! result = db.Connection.QueryAsync<Models.Participant>(query, parameters, db.Transaction)
            return
                Ok result
        with
            | ex -> return Error ex
    }

let addParticipantToEvent (eventId: Guid) email (userId: int option) name department (db: DatabaseContext) =
    let query =
        "
        INSERT INTO Participants
        OUTPUT INSERTED.*
        VALUES (@email, @eventId, @currentEpoch, @cancellationToken, @name, @employeeId, @department);
        "

    let parameters = {|
        EventId = eventId
        CurrentEpoch = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        Email = email
        CancellationToken = Guid.NewGuid()
        Name = name
        Department = department
        EmployeeId = userId
    |}

    try
        db.Connection.QuerySingle<Models.Participant>(query, parameters, db.Transaction)
        |> Ok
    with
        | ex -> Error ex

let getEventQuestions eventId (db: DatabaseContext) =
    let query =
        "
        SELECT [Id]
              ,[EventId]
              ,[Question]
        FROM [ParticipantQuestions]
        WHERE EventId = @eventId
        ORDER BY Id ASC;
        "
    let parameters = {|
        EventId = eventId
    |}

    db.Connection.Query<Models.ParticipantQuestion>(query, parameters, db.Transaction)
    |> Seq.toList

let createParticipantAnswers (participantAnswers: Models.ParticipantAnswer list) (db: DatabaseContext) =
    let answer = List.tryHead participantAnswers
    match answer with
    | None -> Ok []
    | Some answer ->
    let insertQuery =
        "
        INSERT INTO ParticipantAnswers (QuestionId, EventId, Email, Answer)
        VALUES (@QuestionId, @EventId, @Email, @Answer);
        "
    let selectQuery =
        "
        SELECT * FROM ParticipantAnswers WHERE
        QuestionId = @questionId AND EventId = @eventId AND Email = @email;
        "
    let selectParameters = {|
        QuestionId = answer.QuestionId
        EventId = answer.EventId
        Email = answer.Email
    |}

    try
        db.Connection.Execute(insertQuery, participantAnswers |> List.toSeq, db.Transaction) |> ignore
        db.Connection.Query<Models.ParticipantAnswer>(selectQuery, selectParameters, db.Transaction)
        |> Seq.toList
        |> Ok
    with
        | ex -> Error ex

let cancelEvent eventId (db: DatabaseContext) =
    task {
        let cancelQuery =
            "
            UPDATE Events
            SET IsCancelled = 1
            WHERE Id = @eventId;
            "

        let parameters = {|
            EventId = eventId
        |}

        try
            let! _ = db.Connection.ExecuteAsync(cancelQuery, parameters, db.Transaction)
            return Ok ()
        with
            | ex ->
                return Error ex
    }

let deleteEvent eventId (db: DatabaseContext) =
    task {
        let deleteQuery =
            "
            DELETE FROM ParticipantQuestions
            WHERE EventId = @eventId;

            DELETE FROM ParticipantAnswers
            WHERE EventId = @eventId;

            DELETE FROM Events
            WHERE Id = @eventId;
            "

        let parameters = {|
            EventId = eventId
        |}

        try
            let! _ = db.Connection.ExecuteAsync(deleteQuery, parameters, db.Transaction)
            return Ok ()
        with
            | ex ->
                return Error ex
    }

let updateEvent eventId (model: Models.EventWriteModel) (db: DatabaseContext) =
    task {
        let query =
            "
            UPDATE Events
            SET Shortname = NULL
            WHERE ShortName = @shortname AND (EndDate < @now Or IsCancelled = 1)

            UPDATE Events
            SET Title = @title,
                Description = @description,
                Location = @location,
                OrganizerEmail = @organizerEmail,
                StartDate = @startDate,
                StartTime = @startTime,
                EndDate = @endDate,
                EndTime = @endTime,
                MaxParticipants = @maxParticipants,
                OrganizerName = @organizerName,
                OpenForRegistrationTime = @openForRegistrationTime,
                HasWaitingList = @hasWaitingList,
                IsCancelled = @isCancelled,
                IsExternal = @isExternal,
                IsHidden = @isHidden,
                CloseRegistrationTime = @closeRegistrationTime,
                CustomHexColor = @customHexColor,
                Shortname = @shortname,
                Program = @program,
                Offices = @offices
            OUTPUT INSERTED.*
            WHERE Id = @eventId;
            "

        let parameters = {|
            EventId = eventId
            Now = DateTime.Now.Date
            Title = model.Title
            Description = model.Description
            Location = model.Location
            OrganizerEmail = model.OrganizerEmail
            StartDate = DateTimeCustom.customToDateTime model.StartDate.Date
            StartTime = DateTimeCustom.customToTimeSpan model.StartDate.Time
            EndDate = DateTimeCustom.customToDateTime model.EndDate.Date
            EndTime = DateTimeCustom.customToTimeSpan model.EndDate.Time
            MaxParticipants = model.MaxParticipants
            OrganizerName = model.OrganizerName
            OpenForRegistrationTime = model.OpenForRegistrationTime
            HasWaitingList = model.HasWaitingList
            IsCancelled = false
            IsExternal = model.IsExternal
            IsHidden = model.IsHidden
            CloseRegistrationTime = model.CloseRegistrationTime
            CustomHexColor = model.CustomHexColor
            Shortname = model.Shortname
            Program = model.Program
            Offices =
                match model.Offices with
                | Some offices ->
                    if List.isEmpty offices then
                        null
                    else
                        offices
                        |> List.map (fun office -> office.ToString())
                        |> String.concat ","
                | None -> null
        |}

        try
            let! result = db.Connection.QuerySingleAsync<Models.Event>(query, parameters, db.Transaction)
            return Ok result
        with
            | ex ->
                return Error ex
    }


let getEventIdByShortname shortname (db: DatabaseContext) =
    task {
        let query =
            "
            SELECT Id FROM Events
            WHERE Shortname = @shortname
            "
        let parameters = {|
            Shortname = shortname
        |}

        try
            let! result = db.Connection.QuerySingleOrDefaultAsync<Guid>(query, parameters, db.Transaction)
            return
                if result <> Guid.Empty
                then Ok (Some result)
                else Ok None
        with
            | ex -> return Error ex
    }

let doesShortnameExist (shortname: string option) (db: DatabaseContext) =
    task {
        let check_for_existing_shortnames_query =
            "
            SELECT Count(*) FROM Events
            WHERE Shortname = @shortname AND (EndDate > @now AND IsCancelled = 0)
            "

        let parameters = {|
            Shortname = shortname
            Now = DateTime.Now.Date
        |}

        try
            if shortname.IsNone then
                return Ok false
            else
                let! hasExistingEvent = db.Connection.QuerySingleAsync<int>(check_for_existing_shortnames_query, parameters, db.Transaction)
                if hasExistingEvent > 0 then
                    return Ok true
                else
                    return Ok false
        with
            | ex -> return Error ex
    }

let createParticipantQuestions (eventId: Guid) (participantQuestions: string list) (db: DatabaseContext) =
    task {
        let question = List.tryHead participantQuestions

        if not question.IsSome then
            return Ok []
        else
            let insertQuery =
                "
                INSERT INTO ParticipantQuestions (EventId, Question)
                VALUES (@eventId, @question)
                "

            let insertParameters =
                participantQuestions
                |> Seq.map (fun question -> {| EventId = eventId; Question = question |})

            let selectQuery =
                "
                SELECT * FROM ParticipantQuestions
                WHERE EventId = @eventId
                "
            let selectParameters = {|
                EventId = eventId
            |}

            try
                let! _ = db.Connection.ExecuteAsync(insertQuery, insertParameters, db.Transaction)
                let! result = db.Connection.QueryAsync<Models.ParticipantQuestion>(selectQuery, selectParameters, db.Transaction)
                return Ok (result |> List.ofSeq)
            with
                | ex -> return Error ex
    }

let deleteParticipantQuestions (eventId: Guid) (db: DatabaseContext) =
    task {
        let query =
            "
            DELETE FROM ParticipantQuestions
            WHERE EventId = @eventId
            "

        let parameters = {|
            EventId = eventId
        |}

        try
            let! result = db.Connection.ExecuteAsync(query, parameters, db.Transaction)
            return Ok result
        with
            | ex -> return Error ex
    }


let createEvent (writeModel: Models.EventWriteModel) employeeId (db: DatabaseContext) =
    task {
        let update_shortname_and_insert_event =
            "
            UPDATE Events
            SET Shortname = NULL
            WHERE ShortName = @shortname AND (EndDate < @now Or IsCancelled = 1)

            INSERT INTO Events (Id,
                    Title,
                    Description,
                    Location,
                    OrganizerEmail,
                    StartDate,
                    StartTime,
                    EndDate,
                    EndTime,
                    MaxParticipants,
                    OrganizerName,
                    OpenForRegistrationTime,
                    EditToken,
                    HasWaitingList,
                    IsCancelled,
                    IsExternal,
                    OrganizerId,
                    IsHidden,
                    CloseRegistrationTime,
                    CustomHexColor,
                    Shortname,
                    Program,
                    Offices)
            OUTPUT INSERTED.*
            VALUES (@eventId,
                    @title,
                    @description,
                    @location,
                    @organizerEmail,
                    @startDate,
                    @startTime,
                    @endDate,
                    @endTime,
                    @maxParticipants,
                    @organizerName,
                    @openForRegistrationTime,
                    @editToken,
                    @hasWaitingList,
                    @isCancelled,
                    @isExternal,
                    @organizerId,
                    @isHidden,
                    @closeRegistrationTime,
                    @customHexColor,
                    @shortname,
                    @program,
                    @offices)
            "
        let newEventId = Guid.NewGuid()
        let newEditToken = Guid.NewGuid()

        let parameters = {|
            EventId = newEventId
            Title = writeModel.Title
            Description = writeModel.Description
            Location = writeModel.Location
            OrganizerEmail = writeModel.OrganizerEmail
            StartDate = DateTimeCustom.customToDateTime writeModel.StartDate.Date
            StartTime = DateTimeCustom.customToTimeSpan writeModel.StartDate.Time
            EndDate = DateTimeCustom.customToDateTime writeModel.EndDate.Date
            EndTime = DateTimeCustom.customToTimeSpan writeModel.EndDate.Time
            MaxParticipants = writeModel.MaxParticipants
            OrganizerName = writeModel.OrganizerName
            OpenForRegistrationTime = writeModel.OpenForRegistrationTime
            EditToken = newEditToken
            HasWaitingList = writeModel.HasWaitingList
            IsCancelled = false
            IsExternal = writeModel.IsExternal
            OrganizerId = employeeId
            IsHidden = writeModel.IsHidden
            CloseRegistrationTime = writeModel.CloseRegistrationTime
            CustomHexColor = writeModel.CustomHexColor
            Shortname = writeModel.Shortname
            Program = writeModel.Program
            Offices =
                match writeModel.Offices with
                | Some offices ->
                    if List.isEmpty offices then
                        null
                    else
                        offices
                        |> List.map (fun office -> office.ToString())
                        |> String.concat ","
                | None ->
                    null
            Now = DateTime.Now.Date
        |}

        try
            let! result = db.Connection.QuerySingleAsync<Models.Event>(update_shortname_and_insert_event, parameters, db.Transaction)
            return Ok result
        with
            | ex -> return Error ex
    }

let getParticipantForEvent eventId email (db: DatabaseContext) =
    task {
        let query =
            "
            SELECT Name,
                   Email,
                   EmployeeId,
                   EventId,
                   RegistrationTime,
                   CancellationToken
            FROM Participants
            WHERE EventId = @eventId AND Email = @email
            "

        let parameters = {|
            EventId = eventId
            Email = email
        |}

        try
            let! result = db.Connection.QuerySingleOrDefaultAsync<Models.Participant>(query, parameters, db.Transaction)
            if isNull (box result) then
                return Ok None
            else
                return Ok (Some result)
        with
            | ex ->
                return Error ex
    }

let getParticipantsAndAnswersForEvent (eventId: Guid) (db: DatabaseContext) =
    task {
        let query =
            "
            SELECT P.Name,
                   P.Email,
                   P.EmployeeId,
                   P.Department,
                   P.EventId,
                   P.RegistrationTime,
                   PA.QuestionId,
                   PA.EventId,
                   PA.Email,
                   PA.Answer
            FROM Participants P
            LEFT JOIN ParticipantAnswers PA ON PA.EventId = P.EventId AND PA.Email = P.Email
            WHERE P.EventId = @eventId
            ORDER BY RegistrationTime;
            "

        let parameters = {|
            EventId = eventId
        |}

        let participants = Dictionary<Models.Participant, Models.ParticipantAnswer list>()

        try
            let! _ =
                db.Connection.QueryAsync(
                    query,
                    (fun (participant: Models.Participant) (answer: Models.ParticipantAnswer) ->
                            if participants.ContainsKey(participant) && not (answer :> obj = null) then
                                participants[participant] <- participants[participant] @ [answer]
                            else if not (participants.ContainsKey(participant)) && not (answer :> obj = null) then
                                participants.Add(participant, [answer])
                            else
                                participants.Add(participant, [])
                        ),
                    parameters,
                    db.Transaction,
                    splitOn = "QuestionId")

            let result: Models.ParticipantAndAnswers seq =
                participants
                |> Seq.fromDict
                |> Seq.map (fun (x, y) -> { Participant = x; Answers = y })

            return Ok result
        with
            | ex -> return Error ex
    }

let getParticipationsForParticipant email (db: DatabaseContext) =
    task {
        let query =
            "
            SELECT P.Email,
                   P.EventId,
                   P.RegistrationTime,
                   P.EmployeeId,
                   P.Name,
                   P.EmployeeId,
                   A.QuestionId,
                   A.EventId,
                   A.Email,
                   A.Answer
            FROM Participants P LEFT JOIN ParticipantAnswers A on
                P.Email = A.Email AND
                P.EventId = A.EventId
            WHERE P.Email = @email
            "

        let parameters = {|
            Email = email
        |}

        let participants = Dictionary<Models.Participant, Models.ParticipantAnswer list>()

        try
            let! _ =
                db.Connection.QueryAsync(
                    query,
                    (fun (participant: Models.Participant) (answer: Models.ParticipantAnswer) ->
                            if participants.ContainsKey(participant) && not (answer :> obj = null) then
                                participants[participant] <- participants[participant] @ [answer]
                            else if not (participants.ContainsKey(participant)) && not (answer :> obj = null) then
                                participants.Add(participant, [answer])
                            else
                                participants.Add(participant, [])
                        ),
                    parameters,
                    splitOn = "QuestionId")

            let result: Models.ParticipantAndAnswers seq =
                participants
                |> Seq.fromDict
                |> Seq.map (fun (x, y) -> { Participant = x; Answers = y })

            return Ok result
        with
            | ex -> return Error ex
    }

let deleteParticipantFromEvent eventId email (db: DatabaseContext) =
    task {
        let query =
            "
            DELETE FROM Participants
            OUTPUT DELETED.*
            WHERE EventId = @eventId AND Email = @email
            "

        let parameters = {|
            EventId = eventId
            Email = email
        |}

        try
            let! result = db.Connection.QuerySingleAsync<Models.Participant>(query, parameters, db.Transaction)
            return Ok result
        with
            | ex -> return Error ex
    }


let isParticipating eventId email (db: DatabaseContext) =
    task {
        let query =
            "
            WITH participation as (
                    SELECT EventId, Email, RegistrationTime, ROW_NUMBER() OVER (
                    PARTITION BY EventId ORDER BY RegistrationTime
                ) registrationSpot
            FROM Participants
            GROUP BY EventId, RegistrationTime, Email)

            SELECT IIF(myp.RegistrationSpot <= E.MaxParticipants, cast(1 as bit), cast(0 as bit))
            FROM Events E
            LEFT OUTER JOIN participation myp ON E.Id = myp.EventId AND myp.Email = @email
            WHERE E.Id = @eventId
            "
        let parameters = dict [
            "eventId", box eventId
            "email", box email
        ]
        try
            let! result = db.Connection.QuerySingleAsync<bool>(query, parameters, db.Transaction)
            return Ok result
        with
            | ex -> return Error ex
    }
