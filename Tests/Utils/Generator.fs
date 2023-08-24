module Generator

open System
open Bogus
open Models

let private faker = Faker()

let private generateDatePast () : DateTimeCustom.Date =
    let date = faker.Date.Past(10, DateTime.Now.AddDays(-1))
    { Day = date.Day
      Month = date.Month
      Year = date.Year }

let private generateDateFuture () : DateTimeCustom.Date =
    let date = faker.Date.Future(10, DateTime.Now.AddDays(1))
    { Day = date.Day
      Month = date.Month
      Year = date.Year }

let private generateDateSoon () : DateTimeCustom.Date =
    let date = faker.Date.Soon(10, DateTime.Now.AddDays(1))
    { Day = date.Day
      Month = date.Month
      Year = date.Year }

let private generateTimePast (): DateTimeCustom.Time =
    let time = faker.Date.Past(10, DateTime.Now.AddMinutes(-1))
    { Hour = time.Hour
      Minute = time.Minute }

let private generateTimeFuture (): DateTimeCustom.Time =
    let time = faker.Date.Future(10, DateTime.Now.AddMinutes(1))
    { Hour = time.Hour
      Minute = time.Minute }

let private generateTimeSoon () : DateTimeCustom.Time =
    let time = faker.Date.Soon(10, DateTime.Now.AddMinutes(1))
    { Hour = time.Hour
      Minute = time.Minute }

let generateDateTimeCustomPast () : DateTimeCustom.DateTimeCustom =
    { Date = generateDatePast ()
      Time = generateTimePast () }

let private generateDateTimeCustomFuture () : DateTimeCustom.DateTimeCustom =
    { Date = generateDateFuture ()
      Time = generateTimeFuture () }

let private generateDateTimeCustomSoon () : DateTimeCustom.DateTimeCustom =
    { Date = generateDateSoon ()
      Time = generateTimeSoon () }

let generateQuestions numberOfQuestions =
  [ 0 .. numberOfQuestions ]
  |> List.map (fun _ -> { Id = None; Question = faker.Lorem.Sentence()})

let generateEvent () : Models.EventWriteModel =
    let start = DateTime.Now.AddDays(-1)
    { Title = faker.Company.CompanyName()
      Description = faker.Lorem.Paragraph()
      Location = faker.Address.City()
      City =
        if faker.Hacker.Random.Bool() then
            None
        else
            Some(faker.Address.City())
      OrganizerName = $"{faker.Person.FirstName} {faker.Person.LastName}"
      OrganizerEmail = faker.Person.Email
      TargetAudience =
        if faker.Hacker.Random.Bool() then
            None
        else
            Some(faker.Commerce.Department())
      MaxParticipants =
          if faker.Hacker.Random.Bool() then
              None
          else
              Some <| faker.Random.Number(1, 100)
      StartDate = DateTimeCustom.toCustomDateTime start.Date start.TimeOfDay
      EndDate = generateDateTimeCustomFuture ()
      OpenForRegistrationTime = DateTimeOffset(start).AddDays(-1).ToUnixTimeMilliseconds().ToString()
      CloseRegistrationTime =
          if faker.Hacker.Random.Bool() then
              None
          else
              Some(
                  DateTimeOffset(faker.Date.Future(10, refDate = start).Date)
                      .ToUnixTimeMilliseconds()
                      .ToString()
              )
      ParticipantQuestions =
          [ 0 .. faker.Random.Number(0, 5) ]
          |> List.map (fun _ -> { Id = None; Question = faker.Lorem.Sentence()})
      Program =
          if faker.Random.Number(0, 5) <> 0 then
              None
          else
              Some(faker.Lorem.Paragraph()[0..99])
      ViewUrlTemplate =
          if faker.Random.Number(0, 5) <> 0 then
              "{eventId}"
          else
              "{shortname}"
      EditUrlTemplate = "{eventId}{editToken}"
      CancelParticipationUrlTemplate = "/events/{eventId}/cancel/{email}?cancellationToken={cancellationToken}"
      HasWaitingList = faker.Hacker.Random.Bool()
      IsExternal = faker.Hacker.Random.Bool()
      IsHidden =
          if faker.Random.Number(0, 10) = 0 then
              true
          else
              false
      EventType =
          if faker.Hacker.Random.Bool() then
              Sosialt
          else
              Faglig
      Shortname =
          if faker.Random.Number(0, 5) <> 0 then
              None
          else
              Some(faker.Lorem.Paragraph()[0..99])
      CustomHexColor =
          if faker.Random.Number(0, 5) <> 0 then
              None
          else
              Some(faker.Random.Hexadecimal(6)[2..])
      Offices =
          if faker.Random.Bool() then
              if faker.Random.Bool() then
                  Some [ Oslo ]
              else
                  Some [ Oslo; Trondheim ]
          else
              None
    }

let generateEmail () = faker.Internet.Email()

let generateName () = faker.Company.CompanyName()
let generateRandomString () = faker.Lorem.Paragraph()[0..199]

let generateParticipant email (event: Event): Models.ParticipantWriteModel =
    { Name = $"{faker.Name.FirstName()} {faker.Name.LastName()}"
      Department = faker.Company.CompanySuffix()
      ParticipantAnswers =
          event.ParticipantQuestions
          |> Array.toList
          |> List.map (fun (question: ParticipantQuestionWriteModel) -> {
               QuestionId = question.Id.Value
               EventId = Guid.Parse(event.Id)
               Email = email
               Answer = faker.Lorem.Sentence()
          })
      CancelUrlTemplate = "{eventId}{email}{cancellationToken}"
      ViewUrlTemplate = "{eventId}" }
