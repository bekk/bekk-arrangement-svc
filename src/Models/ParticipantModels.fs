module Participant.Models

open System

open Email.Types
open Validation
open UserMessage
open Participant.Types

type Participant =
    { Name: Types.Name
      Email: Email.Types.EmailAddress
      ParticipantAnswers: Types.ParticipantAnswers
      EventId: Event.Types.Id
      RegistrationTime: TimeStamp.TimeStamp
      CancellationToken: Guid
      EmployeeId: Types.EmployeeId }
    static member Create =
        fun name email participantAnswers eventId registrationTime cancellationToken employeeId ->
            { Name = name
              Email = email
              ParticipantAnswers = participantAnswers
              EventId = eventId
              RegistrationTime = registrationTime
              CancellationToken = cancellationToken
              EmployeeId = employeeId }
    static member CreateFromPrimitives =
        fun name email participantAnswers eventId registrationTime cancellationToken employeeId ->
            { Name = name |> Name
              Email = email |> EmailAddress
              ParticipantAnswers = participantAnswers |> ParticipantAnswers
              EventId = eventId |> Event.Types.Id
              RegistrationTime = registrationTime |> TimeStamp.TimeStamp
              CancellationToken = cancellationToken
              EmployeeId = employeeId |> EmployeeId }

type ParticipantAnswerDbModel = {
  QuestionId: int
  EventId: Guid
  Email: string
  Answer: string
}

[<CLIMutable>]
type DbModel =
  { Name: string
    Email: string
    RegistrationTime: int64
    EventId: Guid
    CancellationToken: Guid
    EmployeeId: int option
  }

type ViewModel =
    { Name: string
      Email: string option
      ParticipantAnswers: string list
      EventId: string
      RegistrationTime: int64
      EmployeeId: int option
    }

type NewlyCreatedParticipationViewModel =
    { Participant: ViewModel
      CancellationToken: string
    }

type WriteModel =
    { Name: string
      ParticipantAnswers: string list
      CancelUrlTemplate: string 
    }

type ParticipantsWithWaitingList =
    { attendees: Participant seq
      waitingList: Participant seq
    }

type ParticipantViewModelsWithWaitingList =
    { attendees: ViewModel list
      waitingList: ViewModel list option
    }

type Key = Guid * string

type EditableEventLocalStorage = {
  EventId: Guid
  EditToken: Guid
}
type ParticipationsLocalStorage = {
  EventId: Guid
  Email: string
  CancellationToken: Guid
}
type ViewModelLocalStorage =
  { EditableEvents: EditableEventLocalStorage list
    Participations: ParticipationsLocalStorage list
  }
  
let dbToDomain (dbRecord: DbModel, answers): Participant =
    { Name = Name dbRecord.Name
      Email = EmailAddress dbRecord.Email
      ParticipantAnswers = ParticipantAnswers answers
      EventId = Event.Types.Id dbRecord.EventId
      RegistrationTime = TimeStamp.TimeStamp dbRecord.RegistrationTime
      CancellationToken = dbRecord.CancellationToken
      EmployeeId = EmployeeId dbRecord.EmployeeId
    }

let writeToDomain ((eventId, email): Key) (writeModel: WriteModel) (employeeId: int option): Result<Participant, UserMessage list> =
      Ok Participant.Create 
      <*> Name.Parse writeModel.Name
      <*> EmailAddress.Parse email 
      <*> ParticipantAnswers.Parse writeModel.ParticipantAnswers
      <*> (Event.Types.Id eventId |> Ok) 
      <*> (TimeStamp.now() |> Ok) 
      <*> (Guid.NewGuid() |> Ok)
      <*> Ok (EmployeeId employeeId)


let domainToView (participant: Participant): ViewModel =
    { Name = participant.Name.Unwrap
      Email = Some participant.Email.Unwrap 
      ParticipantAnswers = participant.ParticipantAnswers.Unwrap
      EventId = participant.EventId.Unwrap.ToString()
      RegistrationTime = participant.RegistrationTime.Unwrap
      EmployeeId = participant.EmployeeId.Unwrap 
    }

let domainToDb (participant: Participant): DbModel =
    { Name = participant.Name.Unwrap
      Email = participant.Email.Unwrap
      EventId = participant.EventId.Unwrap
      RegistrationTime = participant.RegistrationTime.Unwrap
      CancellationToken = participant.CancellationToken
      EmployeeId = participant.EmployeeId.Unwrap
    }

let domainToViewWithCancelInfo (participant: Participant): NewlyCreatedParticipationViewModel
    =
    { Participant = domainToView participant
      CancellationToken = participant.CancellationToken.ToString() }

let domainToLocalStorageView events (participations: Participant seq) : ViewModelLocalStorage = 
    let eventToLocalStorage (event: Event.Models.Event) =
                                    { EventId = event.Id.Unwrap
                                      EditToken = event.EditToken
                                    }
    let participationToLocalStorage (participant:Participant) = { EventId=participant.EventId.Unwrap
                                                                  Email= participant.Email.Unwrap
                                                                  CancellationToken=participant.CancellationToken
                                                                }

    { EditableEvents = (events |> Seq.map eventToLocalStorage |> Seq.toList)
      Participations = participations |> Seq.map participationToLocalStorage |> Seq.toList
    }