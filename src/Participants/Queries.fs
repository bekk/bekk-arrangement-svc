namespace ArrangementService.Participant

open System
open Microsoft.AspNetCore.Http
open Dapper.FSharp

open ArrangementService
open ArrangementService.Email
open ArrangementService.DomainModels
open ResultComputationExpression
open ArrangementService.UserMessage
open System.Data.SqlClient

module Queries =
    let participantsTable = "Participants"

    let handleAggregateToSqlException (exn:AggregateException) userMessage = 
          let innerException = exn.InnerException
          match innerException with 
            | :? SqlException as sqlEx ->
                match sqlEx.Number with
                  | 2601 | 2627 ->          // handle constraint error
                      Error [userMessage]
                  | _ ->                    // don't handle any other cases, Deadlock will still be raised so it can be catched by withRetry
                      raise sqlEx
            | ex -> raise ex

    let createParticipant (participant: Participant) (ctx:HttpContext):Result<unit, UserMessage list> =
      try
        insert { table participantsTable
                 value (Models.domainToDb participant)
                }
                |> Database.runInsertQuery ctx
      with  
      | :? AggregateException as ex  -> 
        handleAggregateToSqlException ex (UserMessages.participantDuplicate participant.Email.Unwrap)
      | ex -> reraise()

    let deleteParticipant (participant: Participant) (ctx: HttpContext): Result<unit, UserMessage list> =
        delete { table participantsTable
                 where (eq "EventId" participant.EventId.Unwrap + eq "Email" participant.Email.Unwrap) 
               }
        |> Database.runDeleteQuery ctx
        |> ignore
        Ok ()

    let queryParticipantByKey (eventId: Event.Id, email: EmailAddress) ctx: Result<Participant, UserMessage list> =
        select { table participantsTable
                 where (eq "EventId" eventId.Unwrap + eq "Email" email.Unwrap)}
        |> Database.runSelectQuery ctx
        // TODO: Lage en funksjon som gjør dette. Brukes flere plasser, se over.
        |> Seq.tryHead
        |> function
        | Some participant -> Ok <| Models.dbToDomain participant
        | _ -> Error []

    let queryParticipantionByParticipant (email: EmailAddress) ctx: Participant seq =
        select { table participantsTable
                 where (eq "Email" email.Unwrap)
               }
       |> Database.runSelectQuery ctx
       |> Seq.map Models.dbToDomain

    let queryParticipationsByEmployeeId (employeeId: Event.EmployeeId) ctx: Participant seq =
        select { table participantsTable
                 where (eq "EmployeeId" employeeId.Unwrap)
               }
       |> Database.runSelectQuery ctx
       |> Seq.map Models.dbToDomain

    let queryParticipantsByEventId (eventId: Event.Id) ctx: Participant seq =
        select { table participantsTable
                 where (eq "EventId" eventId.Unwrap)
                 orderBy "RegistrationTime" Asc
               }
        |> Database.runSelectQuery ctx
        |> Seq.map Models.dbToDomain
    
    let getNumberOfParticipantsForEvent (eventId: Event.Id) (ctx: HttpContext) =
        select { table participantsTable
                 count "*" "Value"
                 where (eq "EventId" eventId.Unwrap)
               }
        |> Database.runSelectQuery<{| Value : int |}> ctx
        |> Seq.tryHead
        |> function
        | Some count -> Ok count.Value
        | None -> Error [UserMessages.getParticipantsCountFailed eventId]
