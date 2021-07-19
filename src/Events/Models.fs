namespace ArrangementService.Event

open System
open System.Linq
open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService

open Validation
open DateTime
open Utils
open UserMessage
open ArrangementService.Email
open ArrangementService.DomainModels

type ShortnameDbModel = {
  Shortname: string 
  EventId: Guid
}

type DbModel = 
    { Id: Guid
      Title: string
      Description: string
      Location: string
      OrganizerName: string
      OrganizerEmail: string
      MaxParticipants: int
      StartDate: DateTime
      EndDate: DateTime
      StartTime: TimeSpan
      EndTime: TimeSpan
      OpenForRegistrationTime: int64
      ParticipantQuestion: string option
      HasWaitingList: bool 
      IsCancelled: bool
      IsExternal: bool 
      EditToken: Guid
      OrganizerId: int
    }

type ViewModel =
    { Id: Guid
      Title: string
      Description: string
      Location: string
      OrganizerName: string
      OrganizerEmail: string
      MaxParticipants: int
      StartDate: DateTimeCustom
      EndDate: DateTimeCustom
      OpenForRegistrationTime: int64
      ParticipantQuestion: string option
      HasWaitingList: bool 
      IsCancelled: bool 
      IsExternal: bool
      OrganizerId: int
      IsInThePast: bool
    }

type ViewModelWithEditToken =
    { Event: ViewModel
      EditToken: string
    }

type WriteModel =
    { Title: string
      Description: string
      Location: string
      OrganizerName: string
      OrganizerEmail: string
      MaxParticipants: int
      StartDate: DateTimeCustom
      EndDate: DateTimeCustom
      OpenForRegistrationTime: string
      editUrlTemplate: string
      ParticipantQuestion: string option
      HasWaitingList: bool 
      IsExternal: bool
      Shortname: string option
    }

module Models =

    let writeToDomain
        (id: Key)
        (writeModel: WriteModel)
        (editToken: Guid)
        (isCancelled: bool)
        (organizerId: int)
        : Result<Event, UserMessage list>
        =
        Ok Event.Create 
        <*> (Id id |> Ok) 
        <*> Title.Parse writeModel.Title
        <*> Description.Parse writeModel.Description
        <*> Location.Parse writeModel.Location
        <*> OrganizerName.Parse writeModel.OrganizerName
        <*> EmailAddress.Parse writeModel.OrganizerEmail
        <*> MaxParticipants.Parse writeModel.MaxParticipants
        <*> validateDateRange writeModel.StartDate writeModel.EndDate
        <*> OpenForRegistrationTime.Parse writeModel.OpenForRegistrationTime
        <*> Ok editToken
        <*> ParticipantQuestion.Parse writeModel.ParticipantQuestion
        <*> (writeModel.HasWaitingList |> Ok)
        <*> Ok isCancelled
        <*> (writeModel.IsExternal |> Ok)
        <*> (EmployeeId organizerId |> Ok)

    let dbToDomain (dbRecord: DbModel): Event =
        { Id = Id dbRecord.Id
          Title = Title dbRecord.Title
          Description = Description dbRecord.Description
          Location = Location dbRecord.Location
          OrganizerName = OrganizerName dbRecord.OrganizerName
          OrganizerEmail = EmailAddress dbRecord.OrganizerEmail
          MaxParticipants = MaxParticipants dbRecord.MaxParticipants
          StartDate = toCustomDateTime dbRecord.StartDate dbRecord.StartTime
          EndDate = toCustomDateTime dbRecord.EndDate dbRecord.EndTime
          OpenForRegistrationTime =
              OpenForRegistrationTime dbRecord.OpenForRegistrationTime
          EditToken = dbRecord.EditToken
          ParticipantQuestion = ParticipantQuestion dbRecord.ParticipantQuestion
          HasWaitingList = dbRecord.HasWaitingList
          IsCancelled = dbRecord.IsCancelled
          IsExternal = dbRecord.IsExternal
          OrganizerId = EmployeeId dbRecord.OrganizerId
        }
        
    let domainToDb (domainModel: Event): DbModel =
        { Id = domainModel.Id.Unwrap
          Title = domainModel.Title.Unwrap
          Description = domainModel.Description.Unwrap
          Location = domainModel.Location.Unwrap
          OrganizerName = domainModel.OrganizerName.Unwrap
          OrganizerEmail = domainModel.OrganizerEmail.Unwrap
          MaxParticipants = domainModel.MaxParticipants.Unwrap
          StartDate = customToDateTime domainModel.StartDate.Date
          StartTime = customToTimeSpan domainModel.StartDate.Time
          EndDate = customToDateTime domainModel.EndDate.Date
          EndTime = customToTimeSpan domainModel.EndDate.Time
          OpenForRegistrationTime = domainModel.OpenForRegistrationTime.Unwrap
          EditToken = domainModel.EditToken
          ParticipantQuestion = domainModel.ParticipantQuestion.Unwrap
          HasWaitingList = domainModel.HasWaitingList
          IsCancelled = domainModel.IsCancelled
          IsExternal = domainModel.IsExternal
          OrganizerId = domainModel.OrganizerId.Unwrap
        }
        

    let domainToView (domainModel: Event): ViewModel =
        { Id = domainModel.Id.Unwrap
          Title = domainModel.Title.Unwrap
          Description = domainModel.Description.Unwrap
          Location = domainModel.Location.Unwrap
          OrganizerName = domainModel.OrganizerName.Unwrap
          OrganizerEmail = domainModel.OrganizerEmail.Unwrap
          MaxParticipants = domainModel.MaxParticipants.Unwrap
          StartDate = domainModel.StartDate
          EndDate = domainModel.EndDate
          OpenForRegistrationTime = domainModel.OpenForRegistrationTime.Unwrap
          ParticipantQuestion = domainModel.ParticipantQuestion.Unwrap
          HasWaitingList = domainModel.HasWaitingList 
          IsCancelled = domainModel.IsCancelled 
          IsExternal = domainModel.IsExternal
          OrganizerId = domainModel.OrganizerId.Unwrap
          IsInThePast = domainModel.StartDate <= DateTime.now() 
        }

    let domainToViewWithEditInfo (event: Event): ViewModelWithEditToken =
        { Event = domainToView event
          EditToken = event.EditToken.ToString()
        }
