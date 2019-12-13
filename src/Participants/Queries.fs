namespace ArrangementService.Participants

open Models
open System.Linq

module Queries =

    let queryParticipantByKey (email, id) (participants: DbModel IQueryable) =
        query {
            for participant in participants do
                where (participant.Email = email && participant.EventId = id)
                select (Some participant)
                exactlyOneOrDefault
        }

    let queryParticipantBy email (participants: DbModel IQueryable) =
        query {
            for participant in participants do
                where (participant.Email = email)
                select participant
        } // |> Seq.toList
