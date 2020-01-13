namespace ArrangementService.Participant

open System
open Giraffe

open ArrangementService

open TimeStamp
open Validation
open Database
open Repo
open UserMessage
open ArrangementService.Email
open ArrangementService.DomainModels

  type ViewModel =
      { Email: string
        EventId: Guid
        RegistrationTime: int64 }

  // Empty for now
  type WriteModel = Unit

  type Key = Guid * string

  type TableModel = ArrangementDbContext.dboSchema.``dbo.Participants``
  type DbModel = ArrangementDbContext.``dbo.ParticipantsEntity``

module Models =

    let dbToDomain (dbRecord: DbModel): Participant =
        { Email = EmailAddress dbRecord.Email
          EventId = Event.Id dbRecord.EventId
          RegistrationTime = TimeStamp dbRecord.RegistrationTime }

    let writeToDomain ((id, email): Key) ((): WriteModel): Result<Participant, UserMessage list> =
        Ok Participant.Create
          <*> EmailAddress.Parse email
          <*> (Event.Id id |> Ok)
          <*> (now () |> Ok)

    let updateDbWithDomain (db: DbModel) (participant: Participant) =
        db.Email <- participant.Email.Unwrap
        db.EventId <- participant.EventId.Unwrap
        db.RegistrationTime <- participant.RegistrationTime.Unwrap
        db

    let domainToView (participant: Participant): ViewModel =
        { Email = participant.Email.Unwrap
          EventId = participant.EventId.Unwrap
          RegistrationTime = participant.RegistrationTime.Unwrap }

    let models: Models<DbModel, Participant, ViewModel, WriteModel, Key, TableModel> =
        { key = fun record -> (record.EventId, record.Email)
          table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Participants

          create = fun table -> table.Create()
          delete = fun record -> record.Delete()

          dbToDomain = dbToDomain
          updateDbWithDomain = updateDbWithDomain
          domainToView = domainToView
          writeToDomain = writeToDomain }