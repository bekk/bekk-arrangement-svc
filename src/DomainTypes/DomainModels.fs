namespace ArrangementService.DomainModels

open ArrangementService
open System

type Email =
    { Subject: string
      Message: string
      To: Email.EmailAddress
      CalendarInvite: string option
    }

type Event =
    { Id: Event.Id
      Title: Event.Title
      Description: Event.Description
      Location: Event.Location
      StartDate: DateTimeCustom
      EndDate: DateTimeCustom
      OrganizerName: Event.OrganizerName
      OrganizerEmail: Email.EmailAddress
      OpenForRegistrationTime: Event.OpenForRegistrationTime
      MaxParticipants: Event.MaxParticipants
      EditToken: Guid
      ParticipantQuestion: Event.ParticipantQuestion
      HasWaitingList: bool
      IsCancelled: bool
    }
    static member Create =
        fun id title description location organizerName organizerEmail maxParticipants (startDate, endDate) openForRegistrationTime editToken participantQuestion hasWaitingList isCancelled->
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
              EditToken = editToken
              ParticipantQuestion = participantQuestion
              HasWaitingList = hasWaitingList
              IsCancelled = isCancelled
            }

type Participant =
    { Name: Participant.Name
      Email: Email.EmailAddress
      Comment: Participant.Comment
      EventId: Event.Id
      RegistrationTime: TimeStamp
      CancellationToken: Guid 
    }
    static member Create =
        fun name email comment eventId registrationTime cancellationToken ->
            { Name = name
              Email = email
              Comment = comment
              EventId = eventId
              RegistrationTime = registrationTime
              CancellationToken = cancellationToken
            }
