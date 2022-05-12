module Event.Models

open System

open Utils
open Email.Types
open Validation
open Event.Types
open UserMessage

type Event =
    { Id: Types.Id
      Title: Types.Title
      Description: Types.Description
      Location: Types.Location
      StartDate: DateTimeCustom.DateTimeCustom
      EndDate: DateTimeCustom.DateTimeCustom
      OrganizerName: Types.OrganizerName
      OrganizerEmail: Email.Types.EmailAddress
      OpenForRegistrationTime: Types.OpenForRegistrationTime
      CloseRegistrationTime: Types.CloseRegistrationTime
      ParticipantQuestions: Types.ParticipantQuestions
      MaxParticipants: Types.MaxParticipants
      EditToken: Guid
      HasWaitingList: bool
      IsCancelled: bool
      IsExternal: bool
      IsHidden: bool
      OrganizerId: Types.EmployeeId
      Shortname: Types.Shortname 
      CustomHexColor: Types.CustomHexColor }
    static member Create =
        fun id title description location organizerName organizerEmail maxParticipants (startDate, endDate) openForRegistrationTime closeRegistrationTime editToken participantQuestions hasWaitingList isCancelled isExternal isHidden organizerId shortname hexCode ->
            { Id = id
              Title = title
              Description = description
              Location = location
              OrganizerName = organizerName
              OrganizerEmail = organizerEmail
              MaxParticipants = maxParticipants
              StartDate = startDate
              EndDate = endDate
              OpenForRegistrationTime = openForRegistrationTime
              CloseRegistrationTime = closeRegistrationTime
              EditToken = editToken
              ParticipantQuestions = participantQuestions
              HasWaitingList = hasWaitingList
              IsCancelled = isCancelled
              IsExternal = isExternal
              IsHidden = isHidden
              OrganizerId = organizerId
              Shortname = shortname
              CustomHexColor = hexCode }

// TODO: Rart at denne lever her
type ParticipantQuestionDbModel = {
  Id: int
  EventId: Guid
  Question: string
}

type ViewModel =
    { Id: Guid
      Title: string
      Description: string
      Location: string
      OrganizerName: string
      OrganizerEmail: string
      MaxParticipants: int option
      StartDate: DateTimeCustom.DateTimeCustom
      EndDate: DateTimeCustom.DateTimeCustom
      ParticipantQuestions: string list
      OpenForRegistrationTime: int64 
      CloseRegistrationTime: int64 option
      HasWaitingList: bool 
      IsCancelled: bool 
      IsExternal: bool
      IsHidden: bool
      OrganizerId: int
      Shortname: string option
      CustomHexColor: string option
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
      MaxParticipants: int option
      StartDate: DateTimeCustom.DateTimeCustom
      EndDate: DateTimeCustom.DateTimeCustom
      OpenForRegistrationTime: string
      CloseRegistrationTime: string option
      ParticipantQuestions: string list
      viewUrl: string option
      editUrlTemplate: string
      HasWaitingList: bool 
      IsExternal: bool
      IsHidden: bool
      Shortname: string option
      CustomHexColor: string option
    }
    
[<CLIMutable>]
type DbModel = 
    { Id: Guid
      Title: string
      Description: string
      Location: string
      OrganizerName: string
      OrganizerEmail: string
      MaxParticipants: int option
      StartDate: DateTime
      EndDate: DateTime
      StartTime: TimeSpan
      EndTime: TimeSpan
      OpenForRegistrationTime: int64
      CloseRegistrationTime: int64 option
      HasWaitingList: bool 
      IsCancelled: bool
      IsExternal: bool 
      IsHidden: bool
      EditToken: Guid
      OrganizerId: int
      CustomHexColor: string option
      Shortname: string option
    }
    static member CreateFromWrite =
        fun (employeeId: int, writeModel: WriteModel) ->
            { Id = Guid.NewGuid()
              Title = writeModel.Title
              Description = writeModel.Description
              Location = writeModel.Location
              OrganizerName = writeModel.OrganizerName
              OrganizerEmail = writeModel.OrganizerEmail
              MaxParticipants = writeModel.MaxParticipants
              StartDate = DateTimeCustom.customToDateTime writeModel.StartDate.Date
              EndDate = DateTimeCustom.customToDateTime writeModel.EndDate.Date
              StartTime = DateTimeCustom.customToTimeSpan writeModel.StartDate.Time
              EndTime = DateTimeCustom.customToTimeSpan writeModel.EndDate.Time
              OpenForRegistrationTime = int64 writeModel.OpenForRegistrationTime
              CloseRegistrationTime = Option.map int64 writeModel.CloseRegistrationTime
              HasWaitingList = writeModel.HasWaitingList
              IsCancelled = false
              IsExternal = writeModel.IsExternal
              IsHidden = writeModel.IsHidden
              EditToken = Guid.NewGuid()
              OrganizerId = employeeId
              CustomHexColor = writeModel.CustomHexColor
              Shortname = writeModel.Shortname }
            
[<CLIMutable>]
type ForsideEvent = {
    Id: Guid
    Title: string
    Location: string
    StartDate: DateTime
    EndDate: DateTime
    StartTime: TimeSpan
    EndTime: TimeSpan
    OpenForRegistrationTime: int64 
    CloseRegistrationTime: int64 option
    CustomHexColor: string option
    Shortname: string option
    HasWaitingList: bool
    IsCancelled: bool
    HasRoom: bool
    IsParticipating: bool
    IsWaitlisted: bool
    PositionInWaitlist: int
}

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
    <*> CloseRegistrationTime.Parse writeModel.CloseRegistrationTime
    <*> Ok editToken
    <*> ParticipantQuestions.Parse writeModel.ParticipantQuestions
    <*> (writeModel.HasWaitingList |> Ok)
    <*> Ok isCancelled
    <*> (writeModel.IsExternal |> Ok)
    <*> (writeModel.IsHidden |> Ok)
    <*> (EmployeeId organizerId |> Ok)
    <*> Shortname.Parse writeModel.Shortname
    <*> CustomHexColor.Parse writeModel.CustomHexColor

let dbToDomain (dbRecord: DbModel, participantQuestions: string list): Event =
    { Id = Id dbRecord.Id
      Title = Title dbRecord.Title
      Description = Description dbRecord.Description
      Location = Location dbRecord.Location
      OrganizerName = OrganizerName dbRecord.OrganizerName
      OrganizerEmail = EmailAddress dbRecord.OrganizerEmail
      MaxParticipants = MaxParticipants dbRecord.MaxParticipants
      StartDate = DateTimeCustom.toCustomDateTime dbRecord.StartDate dbRecord.StartTime
      EndDate = DateTimeCustom.toCustomDateTime dbRecord.EndDate dbRecord.EndTime
      OpenForRegistrationTime =
          OpenForRegistrationTime dbRecord.OpenForRegistrationTime
      CloseRegistrationTime =
          CloseRegistrationTime dbRecord.CloseRegistrationTime
      EditToken = dbRecord.EditToken
      HasWaitingList = dbRecord.HasWaitingList
      IsCancelled = dbRecord.IsCancelled
      IsExternal = dbRecord.IsExternal
      IsHidden = dbRecord.IsHidden
      ParticipantQuestions = ParticipantQuestions participantQuestions
      OrganizerId = EmployeeId dbRecord.OrganizerId
      Shortname = Shortname dbRecord.Shortname
      CustomHexColor = CustomHexColor dbRecord.CustomHexColor
    }
    
let domainToDb (domainModel: Event): DbModel =
    { Id = domainModel.Id.Unwrap
      Title = domainModel.Title.Unwrap
      Description = domainModel.Description.Unwrap
      Location = domainModel.Location.Unwrap
      OrganizerName = domainModel.OrganizerName.Unwrap
      OrganizerEmail = domainModel.OrganizerEmail.Unwrap
      MaxParticipants = domainModel.MaxParticipants.Unwrap
      StartDate = DateTimeCustom.customToDateTime domainModel.StartDate.Date
      StartTime = DateTimeCustom.customToTimeSpan domainModel.StartDate.Time
      EndDate = DateTimeCustom.customToDateTime domainModel.EndDate.Date
      EndTime = DateTimeCustom.customToTimeSpan domainModel.EndDate.Time
      OpenForRegistrationTime = domainModel.OpenForRegistrationTime.Unwrap
      CloseRegistrationTime = domainModel.CloseRegistrationTime.Unwrap
      EditToken = domainModel.EditToken
      HasWaitingList = domainModel.HasWaitingList
      IsCancelled = domainModel.IsCancelled
      IsExternal = domainModel.IsExternal
      IsHidden = domainModel.IsHidden
      OrganizerId = domainModel.OrganizerId.Unwrap
      CustomHexColor = domainModel.CustomHexColor.Unwrap
      Shortname = domainModel.Shortname.Unwrap
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
      CloseRegistrationTime = domainModel.CloseRegistrationTime.Unwrap
      HasWaitingList = domainModel.HasWaitingList 
      IsCancelled = domainModel.IsCancelled 
      IsExternal = domainModel.IsExternal
      IsHidden = domainModel.IsHidden
      ParticipantQuestions = domainModel.ParticipantQuestions.Unwrap
      OrganizerId = domainModel.OrganizerId.Unwrap
      Shortname = domainModel.Shortname.Unwrap
      CustomHexColor = domainModel.CustomHexColor.Unwrap
    }

let domainToViewWithEditInfo (event: Event): ViewModelWithEditToken =
    { Event = domainToView event
      EditToken = event.EditToken.ToString()
    }
