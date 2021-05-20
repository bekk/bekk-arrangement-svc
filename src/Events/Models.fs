namespace ArrangementService.Event

open System
open System.Linq
open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService

open Validation
open Database
open Repo
open DateTime
open Utils
open UserMessage
open ArrangementService.Email
open ArrangementService.DomainModels

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
      ParticipantQuestion: string
      HasWaitingList: bool 
      EditToken: Guid
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
      ParticipantQuestion: string
      HasWaitingList: bool 
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
      OpenForRegistrationTime: int64
      editUrlTemplate: string
      ParticipantQuestion: string
      HasWaitingList: bool 
    }

module Models =

    let writeToDomain
        (id: Key)
        (writeModel: WriteModel)
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
        <*> (Guid.NewGuid() |> Ok)
        <*> ParticipantQuestion.Parse writeModel.ParticipantQuestion
        <*> (writeModel.HasWaitingList |> Ok)

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
          HasWaitingList = dbRecord.HasWaitingList }

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
          HasWaitingList = domainModel.HasWaitingList }

    let domainToViewWithEditInfo (event: Event): ViewModelWithEditToken =
        { Event = domainToView event
          EditToken = event.EditToken.ToString() }