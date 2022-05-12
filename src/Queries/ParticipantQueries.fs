module Participant.Queries

open Dapper.FSharp

open Tables
open Email.Types
open Participant.Models
open ResultComputationExpression

let deleteParticipant (participant: Participant): Handler<unit> =
    result {
        let! _res =
            delete { table participantsTable
                     where (eq "EventId" participant.EventId.Unwrap + eq "Email" participant.Email.Unwrap) 
                   }
            |> Database.runDeleteQuery 
        return ()
    }

let private groupParticipants ls =
    ls
    |> Seq.groupBy (fun (participant: DbModel, _) -> (participant.EventId, participant.Email))
    |> Seq.map (fun (_, listOfParticipants) -> 
        let participant, _ = listOfParticipants |> Seq.head 
        let sortedAnswersForParticipant =
            listOfParticipants 
            |> Seq.collect (fun (_, answer) -> match answer with | Some a -> [ a ] | None -> []) 
            |> Seq.sortBy (fun a -> a.QuestionId) 
            |> Seq.map (fun a -> a.Answer) 
            |> List.ofSeq

        (participant , sortedAnswersForParticipant)
    )

let queryParticipantByKey (eventId: Event.Types.Id, email: EmailAddress): Handler<Participant> =
    result {
        let! participants =
            select { table participantsTable
                     leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                     where (eq "Participants.EventId" eventId.Unwrap + eq "Participants.Email" email.Unwrap)}
            |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel>

        let participant =
            participants
            |> groupParticipants
            |> Seq.tryHead

        match participant with
        | Some (participant, answers) ->
            return dbToDomain (participant, answers)
        | _ -> 
            return! Error [ UserMessages.Participants.participationNotFound (eventId, email.Unwrap) ] |> Task.wrap
    }

let createParticipant (participant: Participant): Handler<unit> =
    result {
        let! () =
            queryParticipantByKey (participant.EventId, participant.Email)
            >> Task.map (function
                            | Ok _ -> Error [ UserMessages.Participants.participantDuplicate participant.Email.Unwrap ]
                            | Error _ -> Ok ())

        do! insert { table participantsTable
                     value (domainToDb participant)
                   }
                |> Database.runInsertQuery 
    }

let queryParticipantionByParticipant (email: EmailAddress): Handler<Participant seq> =
    result {
        let! participants =
            select { table participantsTable
                     leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                     where (eq "Participants.Email" email.Unwrap)}
            |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel>

        let groupedParticipants =
            participants
            |> groupParticipants
        
        return Seq.map dbToDomain groupedParticipants
    }

let queryParticipationsByEmployeeId (employeeId: Event.Types.EmployeeId): Handler<Participant seq> =
    result {
        let! participants =
            select { table participantsTable
                     leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                     where (eq "EmployeeId" employeeId.Unwrap)
                   }
            |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel> 

        let groupedParticipants =
            participants
            |> groupParticipants

        return Seq.map dbToDomain groupedParticipants
    }

let queryParticipantsByEventId (eventId: Event.Types.Id): Handler<Participant seq> =
    result {
        let! participants =
            select { table participantsTable
                     leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                     where (eq "Participants.EventId" eventId.Unwrap)
                     orderBy "RegistrationTime" Asc
                   }
            |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel> 

        let groupedParticipants =
            participants
            |> groupParticipants

        return Seq.map dbToDomain groupedParticipants
    }

let queryNumberOfParticipantsForEvent (eventId: Event.Types.Id): Handler<int> =
    result {
        let! participants =
            select { table participantsTable
                     count "*" "Value"
                     where (eq "EventId" eventId.Unwrap)
                   }
            |> Database.runSelectQuery<{| Value : int |}>
        
        let count = Seq.tryHead participants
        match count with
        | Some count -> 
            return count.Value
        | None -> 
            return! Error [UserMessages.Participants.getParticipantsCountFailed eventId] |> Task.wrap
    }

let setAnswers (participant: Participant) =
    result {
        if Seq.isEmpty participant.ParticipantAnswers.Unwrap then
            return ()
        else

        let! questions =
            select {
              table questionsTable
              where (eq "EventId" participant.EventId.Unwrap)
              orderBy "Id" Asc
            } |> Database.runSelectQuery<Event.Models.ParticipantQuestionDbModel>

        do! insert { table answersTable
                     values (participant.ParticipantAnswers.Unwrap 
                            |> Seq.zip questions
                            |> Seq.map (fun (question, answer) -> 
                                {| QuestionId = question.Id
                                   EventId = participant.EventId.Unwrap
                                   Email = participant.Email.Unwrap
                                   Answer = answer
                                |})
                            |> List.ofSeq)
                   } |> Database.runInsertQuery
    }