namespace ArrangementService.Events

open System
open Giraffe

open ArrangementService.Database
open ArrangementService.Repo

module Models =

    type Date =
        { Day: int
          Month: int
          Year: int }

    type Time =
        { Hour: int
          Minute: int }

    type DateTimeCustom =
        { Date: Date
          Time: Time }

    type DomainModel =
        { Id: Guid
          Title: string
          Description: string
          Location: string
          OrganizerEmail: string
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OpenForRegistrationDate: DateTimeCustom }

    type ViewModel =
        { Id: Guid
          Title: string
          Description: string
          Location: string
          OrganizerEmail: string
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OpenForRegistrationDate: DateTimeCustom }

    type WriteModel =
        { Title: string
          Description: string
          Location: string
          OrganizerEmail: string
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OpenForRegistrationDate: DateTimeCustom }

    type TableModel = ArrangementDbContext.dboSchema.``dbo.Events``

    type DbModel = ArrangementDbContext.``dbo.EventsEntity``

    type Key = Guid

    let toCustomDateTime (date: DateTime) (time: TimeSpan): DateTimeCustom =
        { Date =
              { Day = date.Day
                Month = date.Month
                Year = date.Year }
          Time =
              { Hour = time.Hours
                Minute = time.Minutes } }

    let customToDateTime (date: Date): DateTime = DateTime(date.Year, date.Month, date.Day)

    let customToTimeSpan (time: Time): TimeSpan = TimeSpan(time.Hour, time.Minute, 0)

    let dbToDomain (dbRecord: DbModel): DomainModel =
        { Id = dbRecord.Id
          Title = dbRecord.Title
          Description = dbRecord.Description
          Location = dbRecord.Location
          OrganizerEmail = dbRecord.OrganizerEmail
          StartDate = toCustomDateTime dbRecord.StartDate dbRecord.StartTime
          EndDate = toCustomDateTime dbRecord.EndDate dbRecord.EndTime
          OpenForRegistrationDate = toCustomDateTime dbRecord.OpenForRegistrationDate dbRecord.OpenForRegistrationTime }

    let updateDbWithDomain (db: DbModel) (event: DomainModel) =
        db.Title <- event.Title
        db.Description <- event.Description
        db.Location <- event.Location
        db.OrganizerEmail <- event.OrganizerEmail
        db.StartDate <- customToDateTime event.StartDate.Date
        db.StartTime <- customToTimeSpan event.StartDate.Time
        db.EndDate <- customToDateTime event.EndDate.Date
        db.EndTime <- customToTimeSpan event.EndDate.Time
        db.OpenForRegistrationDate <- customToDateTime event.OpenForRegistrationDate.Date
        db.OpenForRegistrationTime <- customToTimeSpan event.OpenForRegistrationDate.Time
        db

    let domainToView (domainModel: DomainModel): ViewModel =
        { Id = domainModel.Id
          Title = domainModel.Title
          Description = domainModel.Description
          Location = domainModel.Location
          OrganizerEmail = domainModel.OrganizerEmail
          StartDate = domainModel.StartDate
          EndDate = domainModel.EndDate
          OpenForRegistrationDate = domainModel.OpenForRegistrationDate }

    let writeToDomain (id: Key) (writeModel: WriteModel): DomainModel =
        { Id = id
          Title = writeModel.Title
          Description = writeModel.Description
          Location = writeModel.Location
          OrganizerEmail = writeModel.OrganizerEmail
          StartDate = writeModel.StartDate
          EndDate = writeModel.EndDate
          OpenForRegistrationDate = writeModel.OpenForRegistrationDate }



    let models: Models<DbModel, DomainModel, ViewModel, WriteModel, Key, TableModel> =
        { key = fun record -> record.Id
          table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Events

          create = fun table -> table.Create()
          delete = fun record -> record.Delete()

          dbToDomain = dbToDomain
          updateDbWithDomain = updateDbWithDomain
          domainToView = domainToView
          writeToDomain = writeToDomain }
