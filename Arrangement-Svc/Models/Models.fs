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

    let participantQuestions (questions: string list) =
        let condition = List.forall (fun (question: string) -> question.Length < 200) questions
        if condition then
            Decode.succeed questions
        else
            Decode.fail "Spørsmål til deltaker kan ha maks 200 tegn"

    let participantAnswers (questions: string list) =
        let condition = List.forall (fun (question: string) -> question.Length < 1000) questions
        if condition then
            Decode.succeed questions
        else
            Decode.fail "Svar kan ha mask 1000 tegn"

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

    let organizerEmail (email: string) =
        if email.Contains '@' then
            Decode.succeed email
        else
            Decode.fail "E-post må inneholde alfakrøll (@)"

type Office =
    | Oslo = 0
    | Trondheim = 1

module Office =
    let toString (office: Office) =
        Enum.GetName<Office>(office)

    let fromString (value: string) =
        let ok, office = Enum.TryParse<Office>(value)
        if ok then
            Some office
        else
            None

    let encoder (office : Office) =
        Encode.string (toString office)

    let decoder : Decoder<Office> =
        Decode.string
        |> Decode.andThen (fun value ->
            match fromString(value) with
            | Some office -> Decode.succeed office
            | None -> Decode.fail "Ugyldig kontor"
        )
        
type EventWriteModel =
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
      Program: string option
      ViewUrl: string option
      EditUrlTemplate: string
      CancelParticipationUrlTemplate: string option
      HasWaitingList: bool
      IsExternal: bool
      IsHidden: bool
      Shortname: string option
      CustomHexColor: string option
      Office: Office option
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
              OrganizerName = get.Required.Field "organizerName"
                          (Decode.string |> Decode.andThen Validate.organizerName)
              OrganizerEmail = get.Required.Field "organizerEmail"
                          (Decode.string |> Decode.andThen Validate.organizerEmail)
              MaxParticipants = get.Optional.Field "maxParticipants"
                                    (Decode.int |> Decode.andThen Validate.maxParticipants)
              StartDate = get.Required.Field "startDate" DateTimeCustom.DateTimeCustom.decoder
              EndDate = get.Required.Field "endDate" DateTimeCustom.DateTimeCustom.decoder
              OpenForRegistrationTime = get.Required.Field "openForRegistrationTime" Decode.string
              CloseRegistrationTime = get.Optional.Field "closeRegistrationTime" Decode.string
              ParticipantQuestions = get.Required.Field "participantQuestions"
                                         (Decode.list Decode.string |> Decode.andThen Validate.participantQuestions)
              Program = get.Optional.Field "program" Decode.string
              ViewUrl = get.Optional.Field "viewUrl" Decode.string
              EditUrlTemplate = get.Required.Field "editUrlTemplate" Decode.string
              CancelParticipationUrlTemplate = get.Optional.Field "cancelParticipationUrlTemplate" Decode.string
              HasWaitingList = get.Required.Field "hasWaitingList" Decode.bool
              IsExternal = get.Required.Field "isExternal" Decode.bool
              IsHidden = get.Required.Field "isHidden" Decode.bool
              CustomHexColor = get.Optional.Field "customHexColor"
                          (Decode.string |> Decode.andThen Validate.customHexColor)
              Shortname =  get.Optional.Field "shortname"
                          (Decode.string |> Decode.andThen Validate.shortname)
              Office = get.Optional.Field "office" Office.decoder })

[<CLIMutable>]
type Event =
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
      Program: string option
      HasWaitingList: bool
      IsCancelled: bool
      IsExternal: bool
      IsHidden: bool
      EditToken: Guid
      OrganizerId: int
      CustomHexColor: string option
      Shortname: string option
      Office: Office option
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
    Office: Office option
}

[<CLIMutable>]
type EventSummary = {
    Id: Guid
    Title: string
    Location: string
    StartDate: DateTime
    IsExternal: bool
    HasRoom: bool
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
                "organizerName", Encode.string event.OrganizerName
                "organizerEmail", Encode.string event.OrganizerEmail
                if event.MaxParticipants.IsSome then
                    "maxParticipants", Encode.int event.MaxParticipants.Value
                "startDate", DateTimeCustom.DateTimeCustom.encoder (DateTimeCustom.toCustomDateTime event.StartDate event.StartTime)
                "endDate", DateTimeCustom.DateTimeCustom.encoder (DateTimeCustom.toCustomDateTime event.EndDate event.EndTime)
                "participantQuestions",
                    participantQuestions
                    |> List.map (fun q -> Encode.string q.Question)
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
                "organizerId", Encode.int event.OrganizerId
                if event.Shortname.IsSome then
                    "shortname", Encode.string event.Shortname.Value
                if event.CustomHexColor.IsSome then
                    "customHexColor", Encode.string event.CustomHexColor.Value
                if event.Office.IsSome then
                    "office", Office.encoder event.Office.Value
                "numberOfParticipants", Encode.int (Option.defaultValue 0 eventAndQuestions.NumberOfParticipants)
            ]
        encoding
        
    let encodeSummary (event: EventSummary) =
        let encoding =
            Encode.object [
                "id", Encode.guid event.Id
                "title", Encode.string event.Title
                "location", Encode.string event.Location
                "startDate", Encode.datetime event.StartDate
                "isExternal", Encode.bool event.IsExternal
                "hasRoom", Encode.bool event.HasRoom
            ]
        encoding

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
                if event.Office.IsSome then
                    "office", Office.encoder event.Office.Value
            ]
        encoding

    let encoderWithEditInfo eventAndQuestions =
        Encode.object [
            "event", encodeEventAndQuestions eventAndQuestions
            "editToken", Encode.guid eventAndQuestions.Event.EditToken
        ]


type ParticipantWriteModel =
    { Name: string
      Department: string
      ParticipantAnswers: string list
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
                                 (Decode.list Decode.string |> Decode.andThen Validate.participantAnswers)
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

type ParticipantAndAnswers = {
    Participant: Participant
    Answers: ParticipantAnswer list
}

type ParticipationsAndWaitlist =
    { Attendees: ParticipantAndAnswers list
      WaitingList: ParticipantAndAnswers list
    }

module Participant =
    let encodeParticipantAndAnswers (participantAndAnswers: ParticipantAndAnswers) =
        let participant = participantAndAnswers.Participant
        let answers = participantAndAnswers.Answers
        Encode.object [
            "name", Encode.string participant.Name
            "email", Encode.string participant.Email
            "participantAnswers",
                answers
                |> List.map (fun a -> Encode.string a.Answer)
                |> Encode.list
            "registrationTime", Encode.int64 participant.RegistrationTime
            "eventId", Encode.guid participant.EventId
            "cancellationToken", Encode.guid participant.CancellationToken
            "employeeId", Encode.option Encode.int participant.EmployeeId
        ]

    let encodeWithCancelInfo (participant: Participant) (answers: ParticipantAnswer list) =
        let participantAndAnswers = {Participant = participant; Answers = answers }
        Encode.object [
            "participant", encodeParticipantAndAnswers participantAndAnswers
            "cancellationToken", Encode.guid participantAndAnswers.Participant.CancellationToken
        ]

    let encodeToLocalStorage (participant: Participant) =
        Encode.object [
            "eventId", Encode.guid participant.EventId
            "email", Encode.string participant.Email
            "cancellationToken", Encode.guid participant.CancellationToken
        ]

    let encodeWithLocalStorage (eventAndQuestions: EventAndQuestions list) (participations: Participant list) =
        Encode.object [
           "editableEvents", eventAndQuestions |> List.map Event.encoderWithEditInfo |> Encode.list
           "participations", participations |> List.map encodeToLocalStorage |> Encode.list
        ]

    let encodeParticipationsAndWaitlist (participationsAndWaitlist: ParticipationsAndWaitlist) =
        Encode.object [
            "attendees",
                participationsAndWaitlist.Attendees
                |> List.map encodeParticipantAndAnswers
                |> Encode.list
            "waitingList",
                participationsAndWaitlist.WaitingList
                |> List.map encodeParticipantAndAnswers
                |> Encode.list
        ]
