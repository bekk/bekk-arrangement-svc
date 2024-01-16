module Models

open System
open Thoth.Json.Net

[<CLIMutable>]
type ParticipantQuestion = {
  Id: int
  EventId: Guid
  Question: string
}

[<CLIMutable>]
type ParticipantAnswer = {
  QuestionId: int
  EventId: Guid
  Email: string
  Answer: string
}

module ParticipantAnswer =
    let decoder : Decoder<ParticipantAnswer> =
        Decode.object (fun get -> {
          QuestionId = get.Required.Field "questionId" Decode.int
          EventId = get.Required.Field "eventId" Decode.guid
          Email = get.Required.Field "email" Decode.string
          Answer = get.Required.Field "answer" Decode.string
        })

    let validate (questions: ParticipantAnswer list) =
        let condition = List.forall (fun (answer: ParticipantAnswer) -> answer.Answer.Length < 1000) questions
        if condition then
            Decode.succeed questions
        else
            Decode.fail "Svar kan ha maks 1000 tegn"


module Validate =
    let private containsChars (toTest: string) (chars: string) =
        Seq.exists (fun c -> Seq.contains c chars) toTest
    let title (title: string) =
        if title.Length < 3 then
            Decode.fail "Tittel må ha minst 3 tegn"
        else if title.Length > 60 then
            Decode.fail "Tittel kan ha maks 60 tegn"
        else
            Decode.succeed title

    let description (description: string) =
        if description.Length < 3 then
            Decode.fail "Beskrivelse må ha minst 3 tegn"
        else
            Decode.succeed description

    let program (program: string ) =
        if program.Length < 5 then
            Decode.fail "Programmet må ha minst 5 tegn"
        else
            Decode.succeed program

    let location (location: string) =
        if location.Length < 3 then
            Decode.fail "Tittel må ha minst 3 tegn"
        else if location.Length > 60 then
            Decode.fail "Tittel kan ha maks 60 tegn"
        else
            Decode.succeed location

    let organizerName (organizerName: string) =
        if organizerName.Length < 3 then
            Decode.fail "Navn må ha minst 3 tegn"
        else if organizerName.Length > 60 then
            Decode.fail "Navn kan ha maks 50 tegn"
        else
            Decode.succeed organizerName

    let maxParticipants (maxParticipants: int) =
        if maxParticipants >= 0 then
            Decode.succeed maxParticipants
        else
            Decode.fail "Maks antall påmeldte kan ikke være negativt"

    let shortname (shortname: string) =
        match shortname with
        | x when x.Length = 0 -> Decode.fail "URL Kortnavn kan ikke være en tom streng"
        | x when x.Length > 200 -> Decode.fail "URL Kortnavn kan ha maks 200 tegn"
        | x when containsChars x "/?#" -> Decode.fail "URL kortnavn kan ikke inneholde reserverte tegn: / ? #"
        | x -> Decode.succeed x

    let customHexColor (hexColor: string) =
        match hexColor with
        | x when containsChars x "#" -> Decode.fail "Hex-koden trenger ikke '#', foreksempel holder det med 'ffaa00' for gul"
        | x when x.Length < 6 -> Decode.fail "Hex-koden må ha nøaktig 6 tegn"
        | x when x.Length > 6 -> Decode.fail "Hex-koden må ha nøaktig 6 tegn"
        | x when not <| Seq.forall Uri.IsHexDigit x -> Decode.fail "Ugyldig tegn, hex-koden må bestå av tegn mellom a..f og 0..9"
        | x -> Decode.succeed x

    let city (city: string) =
        if String.length city > 200 then
            Decode.fail "By kan ha maks 200 tegn"
        else
            Decode.succeed city

    let targetAudience (targetAudience: string) =
        if String.length targetAudience > 200 then
            Decode.fail "Deltakergruppe kan ha maks 200 tegn"
        else
            Decode.succeed targetAudience

    let organizerEmail (email: string) =
        if email.Contains '@' then
            Decode.succeed email
        else
            Decode.fail "E-post må inneholde alfakrøll (@)"



[<AutoOpen>]
module Office =
    type Office =
        | Oslo
        | Trondheim
        with override this.ToString() =
                match this with
                | Oslo -> "Oslo"
                | Trondheim -> "Trondheim"
    let decoder: Decoder<Office> =
        Decode.string
        |> Decode.andThen (fun value ->
            match value with
            | "Oslo" -> Decode.succeed Oslo
            | "Trondheim" -> Decode.succeed Trondheim
            | _ -> Decode.fail "Ugyldig kontor"
        )
    let encoder (office: Office) = Encode.string (office.ToString())

[<AutoOpen>]
module EventType =
    type EventType =
        | Faglig
        | Sosialt
        with override this.ToString() =
                match this with
                | Faglig -> "Faglig"
                | Sosialt -> "Sosialt"
    let decoder: Decoder<EventType> =
        Decode.string
        |> Decode.andThen (fun value ->
            match value with
            | "Faglig" -> Decode.succeed Faglig
            | "Sosialt" -> Decode.succeed Sosialt
            | _ -> Decode.fail "Ugyldig arrangementstype"
        )
    let encoder (eventType: EventType) = Encode.string (eventType.ToString())

type ParticipantQuestionWriteModel =
    { Id: int option
      Question: string }

module ParticipantQuestionWriteModel =
    let decoder : Decoder<ParticipantQuestionWriteModel> =
        Decode.object (fun get -> {
           Id = get.Optional.Field "id" Decode.int
           Question = get.Required.Field "question" Decode.string
        })

    let validateParticipantQuestions (questions: ParticipantQuestionWriteModel list) =
        let condition = List.forall (fun question -> question.Question.Length < 200) questions
        if condition then
            Decode.succeed questions
        else
            Decode.fail "Spørsmål til deltaker kan ha maks 200 tegn"

type EventWriteModel =
    { Title: string
      Description: string
      Location: string
      City: string option
      OrganizerName: string
      OrganizerEmail: string
      TargetAudience: string option
      MaxParticipants: int option
      StartDate: DateTimeCustom.DateTimeCustom
      EndDate: DateTimeCustom.DateTimeCustom
      OpenForRegistrationTime: string
      CloseRegistrationTime: string option
      ParticipantQuestions: ParticipantQuestionWriteModel list
      Program: string option
      ViewUrlTemplate: string
      EditUrlTemplate: string
      CancelParticipationUrlTemplate: string
      HasWaitingList: bool
      IsExternal: bool
      IsPubliclyAvailable: bool
      IsHidden: bool
      EventType: EventType
      Shortname: string option
      CustomHexColor: string option
      Offices: Option<Office list>
    }

module EventWriteModel =
    let decoder : Decoder<EventWriteModel> =
        Decode.object (fun get ->
            { Title = get.Required.Field "title"
                       (Decode.string |> Decode.andThen Validate.title)
              Description = get.Required.Field "description"
                           (Decode.string |> Decode.andThen Validate.description)
              Location = get.Required.Field "location"
                          (Decode.string |> Decode.andThen Validate.location)
              City = get.Optional.Field "city"
                          (Decode.string |> Decode.andThen Validate.city)
              OrganizerName = get.Required.Field "organizerName"
                          (Decode.string |> Decode.andThen Validate.organizerName)
              OrganizerEmail = get.Required.Field "organizerEmail"
                          (Decode.string |> Decode.andThen Validate.organizerEmail)
              TargetAudience = get.Optional.Field "targetAudience"
                          (Decode.string |> Decode.andThen Validate.targetAudience)
              MaxParticipants = get.Optional.Field "maxParticipants"
                                    (Decode.int |> Decode.andThen Validate.maxParticipants)
              StartDate = get.Required.Field "startDate" DateTimeCustom.DateTimeCustom.decoder
              EndDate = get.Required.Field "endDate" DateTimeCustom.DateTimeCustom.decoder
              OpenForRegistrationTime = get.Required.Field "openForRegistrationTime" Decode.string
              CloseRegistrationTime = get.Optional.Field "closeRegistrationTime" Decode.string
              ParticipantQuestions = get.Required.Field "participantQuestions"
                                         (Decode.list ParticipantQuestionWriteModel.decoder |> Decode.andThen ParticipantQuestionWriteModel.validateParticipantQuestions)
              Program = get.Optional.Field "program" Decode.string
              ViewUrlTemplate = get.Required.Field "viewUrlTemplate" Decode.string
              EditUrlTemplate = get.Required.Field "editUrlTemplate" Decode.string
              CancelParticipationUrlTemplate = get.Required.Field "cancelParticipationUrlTemplate" Decode.string
              HasWaitingList = get.Required.Field "hasWaitingList" Decode.bool
              IsExternal = get.Required.Field "isExternal" Decode.bool
              IsHidden = get.Required.Field "isHidden" Decode.bool
              IsPubliclyAvailable = get.Required.Field "isPubliclyAvailable" Decode.bool
              EventType = get.Required.Field "eventType" EventType.decoder
              CustomHexColor = get.Optional.Field "customHexColor"
                          (Decode.string |> Decode.andThen Validate.customHexColor)
              Shortname =  get.Optional.Field "shortname"
                          (Decode.string |> Decode.andThen Validate.shortname)
              Offices = get.Optional.Field "offices" (Decode.list Office.decoder) })



[<CLIMutable>]
type Event =
    { Id: Guid
      Title: string
      Description: string
      Location: string
      City: string option
      OrganizerName: string
      OrganizerEmail: string
      TargetAudience: string option
      MaxParticipants: int option
      StartDate: DateTime
      EndDate: DateTime
      StartTime: TimeSpan
      EndTime: TimeSpan
      OpenForRegistrationTime: int64
      CloseRegistrationTime: int64 option
      Program: string option
      HasWaitingList: bool
      IsCancelled: bool
      IsExternal: bool
      IsHidden: bool
      IsPubliclyAvailable: bool
      EventType: EventType
      EditToken: Guid
      OrganizerId: int
      CustomHexColor: string option
      Shortname: string option
      Offices: string option
    }
    
type EventAndQuestions = {
    Event: Event
    NumberOfParticipants: int option
    Questions: ParticipantQuestion list
}

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
    MaxParticipants: int option
    CustomHexColor: string option
    Shortname: string option
    HasWaitingList: bool
    IsParticipating: bool
    HasRoom: bool
    IsWaitlisted: bool
    PositionInWaitlist: int
    Offices: string option
}

[<CLIMutable>]
type OfficeEvent = {
    Id: string
    Title: string
    Description: string
    Types: string list
    Themes: string list
    StartTime: string
    EndTime: string
    ContactPerson: string
    ModifiedAt: DateTime
    CreatedAt: DateTime
    Location: string
    City: string
}

[<CLIMutable>]
type EventSummary = {
    Id: Guid
    Title: string
    StartDate: DateTime
    IsExternal: bool
    IsPubliclyAvailable: bool
    EventType: EventType
    City: string option
    TargetAudience: string option
}

module Event =
    let encodeEventAndQuestions (eventAndQuestions: EventAndQuestions) =
        let event = eventAndQuestions.Event
        let participantQuestions = eventAndQuestions.Questions
        let encoding =
            Encode.object [
                "id", Encode.guid event.Id
                "title", Encode.string event.Title
                "description", Encode.string event.Description
                "location", Encode.string event.Location
                if event.City.IsSome then
                    "city", Encode.string event.City.Value
                "organizerName", Encode.string event.OrganizerName
                "organizerEmail", Encode.string event.OrganizerEmail
                if event.TargetAudience.IsSome then
                    "targetAudience", Encode.string event.TargetAudience.Value
                if event.MaxParticipants.IsSome then
                    "maxParticipants", Encode.int event.MaxParticipants.Value
                "startDate", DateTimeCustom.DateTimeCustom.encoder (DateTimeCustom.toCustomDateTime event.StartDate event.StartTime)
                "endDate", DateTimeCustom.DateTimeCustom.encoder (DateTimeCustom.toCustomDateTime event.EndDate event.EndTime)
                "participantQuestions",
                    participantQuestions
                    |> List.map (fun q ->
                        Encode.object [
                            "id", Encode.int q.Id
                            "question", Encode.string q.Question
                        ])
                    |> Encode.list
                if event.Program.IsSome then
                    "program", Encode.string event.Program.Value
                "openForRegistrationTime", Encode.int64 event.OpenForRegistrationTime
                if event.CloseRegistrationTime.IsSome then
                    "closeRegistrationTime", Encode.int64 event.CloseRegistrationTime.Value
                "hasWaitingList", Encode.bool event.HasWaitingList
                "isCancelled", Encode.bool event.IsCancelled
                "isExternal", Encode.bool event.IsExternal
                "isHidden", Encode.bool event.IsHidden
                "isPubliclyAvailable", Encode.bool event.IsPubliclyAvailable
                "eventType", EventType.encoder event.EventType
                "organizerId", Encode.int event.OrganizerId
                if event.Shortname.IsSome then
                    "shortname", Encode.string event.Shortname.Value
                if event.CustomHexColor.IsSome then
                    "customHexColor", Encode.string event.CustomHexColor.Value
                if event.Offices.IsSome then
                    "offices",
                        event.Offices.Value.Split ","
                        |> Seq.map Encode.string
                        |> Encode.seq
                "numberOfParticipants", Encode.int (Option.defaultValue 0 eventAndQuestions.NumberOfParticipants)
            ]
        encoding

    let encodeSkjerEventSummary (event: EventSummary) =
        let encoding =
            Encode.object [
                "id", Encode.guid event.Id
                "title", Encode.string event.Title
                "startDate", Encode.datetime event.StartDate
                "isExternal", Encode.bool event.IsExternal
                "isPubliclyAvailable", Encode.bool event.IsPubliclyAvailable
                "eventType", EventType.encoder event.EventType
                if event.City.IsSome then
                    "city", Encode.string event.City.Value
                if event.TargetAudience.IsSome then
                    "targetAudience", Encode.string event.TargetAudience.Value
            ]
        encoding
        
    let encodeOfficeEventSummary (event: OfficeEvent) =
        Encode.object [
            "id", Encode.guid (Guid.NewGuid())
            "title", Encode.string event.Title
            "startDate", Encode.datetime (DateTime.Parse event.StartTime)
            "isExternal", Encode.bool false
            "isPubliclyAvailable", Encode.bool true
            "eventType", EventType.encoder EventType.Faglig
            "city", Encode.string event.City
            "targetAudience", Encode.string "Bekkere"
        ]
    
    let encodeForside (event: ForsideEvent) =
        let encoding =
            Encode.object [
                "id", Encode.guid event.Id
                "title", Encode.string event.Title
                "location", Encode.string event.Location
                "startDate", Encode.datetime event.StartDate
                "endDate", Encode.datetime event.EndDate
                "startTime", Encode.timespan event.StartTime
                "endTime", Encode.timespan event.EndTime
                "openForRegistrationTime", Encode.int64 event.OpenForRegistrationTime
                if event.CloseRegistrationTime.IsSome then
                    "closeRegistrationTime", Encode.int64 event.CloseRegistrationTime.Value
                if event.MaxParticipants.IsSome then
                    "maxParticipants", Encode.int event.MaxParticipants.Value
                if event.CustomHexColor.IsSome then
                    "customHexColor", Encode.string event.CustomHexColor.Value
                if event.Shortname.IsSome then
                    "shortname", Encode.string event.Shortname.Value
                "hasWaitingList", Encode.bool event.HasWaitingList
                "hasRoom", Encode.bool event.HasRoom
                "isParticipating", Encode.bool event.IsParticipating
                "isWaitlisted", Encode.bool event.IsWaitlisted
                "positionInWaitlist", Encode.int event.PositionInWaitlist
                if event.Offices.IsSome then
                    "offices",
                        event.Offices.Value.Split ","
                        |> Seq.map Encode.string
                        |> Encode.seq
            ]
        encoding

    let encoderWithEditInfo eventAndQuestions =
        Encode.object [
            "event", encodeEventAndQuestions eventAndQuestions
            "editToken", Encode.guid eventAndQuestions.Event.EditToken
        ]

    let encodeEditableEvents eventAndQuestions =
        Encode.object [
            "eventId", Encode.guid eventAndQuestions.Event.Id
            "editToken", Encode.guid eventAndQuestions.Event.EditToken
        ]
type ParticipantWriteModel =
    { Name: string
      Department: string
      ParticipantAnswers: ParticipantAnswer list
      ViewUrlTemplate: string
      CancelUrlTemplate: string
    }

module ParticipantWriteModel =
  let decoder: Decoder<ParticipantWriteModel> =
    Decode.object (fun get ->
      {
        Name = get.Required.Field "name"
                   (Decode.string |> Decode.andThen Validate.organizerName)
        Department = get.Required.Field "department" Decode.string
        ParticipantAnswers = get.Required.Field "participantAnswers"
                                 (Decode.list ParticipantAnswer.decoder |> Decode.andThen ParticipantAnswer.validate)
        ViewUrlTemplate = get.Required.Field "viewUrlTemplate" Decode.string
        CancelUrlTemplate = get.Required.Field "cancelUrlTemplate" Decode.string
      })

[<CLIMutable>]
type Participant =
  { Name: string
    Email: string
    Department: string option
    RegistrationTime: int64
    EventId: Guid
    CancellationToken: Guid
    EmployeeId: int option
  }

[<CLIMutable>]
type QuestionAndAnswer = {
    QuestionId: int
    Question: string
    Answer: string
}
type ParticipantAndAnswers = {
    Participant: Participant
    QuestionAndAnswers: QuestionAndAnswer list
}
type ParticipationsAndWaitlist =
    { Attendees: ParticipantAndAnswers list
      WaitingList: ParticipantAndAnswers list
    }

let createQuestionAndAnswer (questions: ParticipantQuestion list) (answers: ParticipantAnswer list) =
    List.map (fun (answer: ParticipantAnswer) ->
        let question: ParticipantQuestion = List.find (fun q -> q.Id = answer.QuestionId) questions
        { QuestionId = question.Id
          Question = question.Question
          Answer = answer.Answer
        }) answers

module Participant =
    let encodeQuestionAndAnswer eventId email (questionAndAnswer: QuestionAndAnswer) =
        Encode.object [
            "questionId", Encode.int questionAndAnswer.QuestionId
            "eventId", Encode.guid eventId
            "email", Encode.string email
            "question", Encode.string questionAndAnswer.Question
            "answer", Encode.string questionAndAnswer.Answer
    ]
    let encodeParticipantAndAnswers (useQuestions: bool) (participantAndAnswers: ParticipantAndAnswers) =
        let participant = participantAndAnswers.Participant
        let questionAndAnswers = participantAndAnswers.QuestionAndAnswers
        Encode.object [
            "name", Encode.string participant.Name
            "email", Encode.string participant.Email
            "questionAndAnswers", if useQuestions then
                                    questionAndAnswers
                                    |> List.map (encodeQuestionAndAnswer participantAndAnswers.Participant.EventId participantAndAnswers.Participant.Email)
                                    |> Encode.list
                                    else
                                    null
            "registrationTime", Encode.int64 participant.RegistrationTime
            "eventId", Encode.guid participant.EventId
            "cancellationToken", Encode.guid participant.CancellationToken
            "employeeId", Encode.option Encode.int participant.EmployeeId
        ]

    let encodeWithCancelInfo (participant: Participant) (questionAndAnswers: QuestionAndAnswer list) =
        let participantAndAnswers = {Participant = participant; QuestionAndAnswers = questionAndAnswers }
        Encode.object [
            "participant", encodeParticipantAndAnswers true participantAndAnswers //a single participant registering their own answers can see them?
            "cancellationToken", Encode.guid participantAndAnswers.Participant.CancellationToken
        ]

    let encodeToLocalStorage (participantAndAnswers: ParticipantAndAnswers) =
        Encode.object [
            "eventId", Encode.guid participantAndAnswers.Participant.EventId
            "email", Encode.string participantAndAnswers.Participant.Email
            "cancellationToken", Encode.guid participantAndAnswers.Participant.CancellationToken
            "questionAndAnswers", participantAndAnswers.QuestionAndAnswers
                       |> List.map (encodeQuestionAndAnswer participantAndAnswers.Participant.EventId participantAndAnswers.Participant.Email)
                       |> Encode.list
        ]

    let encodeWithLocalStorage (eventAndQuestions: EventAndQuestions list) (participations: ParticipantAndAnswers list) =
        Encode.object [
           "editableEvents", eventAndQuestions |> List.map Event.encodeEditableEvents |> Encode.list
           "participations", participations |> List.map encodeToLocalStorage |> Encode.list
        ]

    let encodeParticipationsAndWaitlist (participationsAndWaitlist: ParticipationsAndWaitlist) (showQuestions: bool) =
        Encode.object [
            "attendees",
                participationsAndWaitlist.Attendees
                |> List.map (encodeParticipantAndAnswers showQuestions)
                |> Encode.list
            "waitingList",
                participationsAndWaitlist.WaitingList
                |> List.map (encodeParticipantAndAnswers showQuestions)
                |> Encode.list
        ]
