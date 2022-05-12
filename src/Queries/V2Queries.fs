module V2.Queries

open System
open Dapper
open Microsoft.Data.SqlClient

let getEventsForForside (email: string) (transaction: SqlTransaction) =
    task {
        let query =
            "
            WITH participation as (
                -- If 'mySpot' is null, the email is not registered for participation
                SELECT q.EventId, COUNT(q.EventId) AS peopleInFront, IIF(mySpot IS NULL, 0, 1) AS isPaameldt
                FROM (SELECT p.EventId AS EventId, p.RegistrationTime AS regTime, participation.RegistrationTime AS mySpot
                      FROM (SELECT EventId, RegistrationTime FROM Participants) p
                               LEFT JOIN (SELECT EventID, RegistrationTime
                                          FROM Participants
                                          WHERE Email = @email) participation ON p.EventId = participation.EventId) q
                     -- If Email is not participating, set their registration to maxInt to count all participants as infront.
                where regTime < IIF(mySpot IS NULL, CAST(0x7FFFFFFFFFFFFFFF AS bigint), mySpot)
                group by q.EventId, mySpot)
            SELECT Id,
                   Title,
                   Location,
                   StartDate,
                   EndDate,
                   StartTime,
                   EndTime,
                   OpenForRegistrationTime,
                   CloseRegistrationTime,
                   CustomHexColor,
                   Shortname,
                   HasWaitingList,
                   IsCancelled,
                   IIF(pn.peopleInFront > MaxParticipants, 0, 1) as hasRoom,
                   IIF(pn.isPaameldt IS NULL, 0, isPaameldt) as isParticipating,
                   IIF(e.HasWaitingList = 1 AND pn.peopleInFront > E.MaxParticipants AND pn.isPaameldt = 1, 1, 0) as isWaitlisted,
                   IIF(e.HasWaitingList = 1 AND pn.peopleInFront > E.MaxParticipants AND pn.isPaameldt = 1, ((pn.peopleInFront - E.MaxParticipants) + 1), 0) as positionInWaitlist
            FROM Events AS e
                     LEFT JOIN participation AS pn ON e.Id = pn.EventId
            WHERE e.EndDate > (GETDATE()) AND e.IsHidden = 0;
            "
            
        let parameters = dict [
            "email", box email
        ]
        
        try
            let! result = transaction.Connection.QueryAsync<Event.Models.ForsideEvent>(query, parameters, transaction)
            return Ok (Seq.toList result)
        with
        | ex ->
            return Error $"Klarer ikke hente arrangementer. Feil: {ex}"
    }

let getEvent (eventId: Guid) (transaction: SqlTransaction) =
    let query =
        "
        SELECT [Id]
              ,[Title]
              ,[Description]
              ,[Location]
              ,[OrganizerEmail]
              ,[StartDate]
              ,[StartTime]
              ,[EndDate]
              ,[EndTime]
              ,[MaxParticipants]
              ,[OrganizerName]
              ,[OpenForRegistrationTime]
              ,[EditToken]
              ,[HasWaitingList]
              ,[IsCancelled]
              ,[IsExternal]
              ,[OrganizerId]
              ,[IsHidden]
              ,[CloseRegistrationTime]
              ,[CustomHexColor]
          FROM [Events]
          WHERE Id = @eventId;
        "
    let parameters = dict [
        "eventId", box eventId
    ]
    
    try
        Ok (transaction.Connection.QuerySingle<Event.Models.DbModel>(query, parameters, transaction))
    with
    | ex -> Error $"Finner ikke arrangement med id: {eventId}. Feil: {ex}"
    
let getNumberOfParticipantsForEvent (eventId: Guid) (transaction: SqlTransaction) =
    let query =
        "
        SELECT COUNT(*) AS NumberOfParticipants
        FROM [Participants]
        WHERE EventId = @eventId;
        "
    let parameters = dict [
        "eventId", box (eventId.ToString())
    ]
    
    transaction.Connection.QuerySingle<int>(query, parameters, transaction)
        
    
let addParticipantToEvent (eventId: Guid) email (userId: int option) name (transaction: SqlTransaction) =
    let query =
        "
        INSERT INTO Participants
        OUTPUT INSERTED.*
        VALUES (@email, @eventId, @currentEpoch, @cancellationToken, @name, @employeeId);
        "
        
    let parameters = dict [
        "eventId", box (eventId.ToString())
        "currentEpoch", box (DateTimeOffset.Now.ToUnixTimeMilliseconds())
        "email", box email
        "cancellationToken", box (Guid.NewGuid().ToString()) 
        "name", box name 
        "employeeId", if userId.IsSome then box userId.Value else box null
    ]
    
    try
        transaction.Connection.QuerySingle<Participant.Models.DbModel>(query, parameters, transaction)
        |> Ok
    with
        | ex -> Error $"Kunne ikke legge til deltakeren. Feil {ex}"
    
let getEventQuestions eventId (transaction: SqlTransaction) =
    let query =
        "
        SELECT [Id]
              ,[EventId]
              ,[Question]
        FROM [ParticipantQuestions]
        WHERE EventId = @eventId
        ORDER BY Id ASC;
        "
    let parameters = dict [
        "eventId", box (eventId.ToString())
    ]
    
    transaction.Connection.Query<Event.Models.ParticipantQuestionDbModel>(query, parameters, transaction)
    |> Seq.toList
        
let createParticipantAnswers (participantAnswers: Participant.Models.ParticipantAnswerDbModel list) (transaction: SqlTransaction) =
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
    let selectParameters = dict [
        "questionId", box answer.QuestionId
        "eventId", box answer.EventId
        "email", box answer.Email
    ]
    
    try
        transaction.Connection.Execute(insertQuery, participantAnswers |> List.toSeq, transaction) |> ignore
        transaction.Connection.Query<Participant.Models.ParticipantAnswerDbModel>(selectQuery, selectParameters, transaction)
        |> Seq.toList
        |> Ok
    with
        | ex -> Error $"Kunne ikke legge til svar til deltakeren. Feil {ex}"
    