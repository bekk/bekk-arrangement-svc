module Generator

open System
open Bogus

let private faker = Faker()

let private generateDatePast () : DateTimeCustom.Date =
    let date = faker.Date.Past(10, DateTime.Now)
    { Day = date.Day
      Month = date.Month
      Year = date.Year }

let private generateDateFuture () : DateTimeCustom.Date =
    let date = faker.Date.Future(10, DateTime.Now)
    { Day = date.Day
      Month = date.Month
      Year = date.Year }

let private generateDateSoon () : DateTimeCustom.Date =
    let date = faker.Date.Soon(10, DateTime.Now)
    { Day = date.Day
      Month = date.Month
      Year = date.Year }

let private generateTimePast (): DateTimeCustom.Time =
    let time = faker.Date.Past(10, DateTime.Now)
    { Hour = time.Hour
      Minute = time.Minute }

let private generateTimeFuture (): DateTimeCustom.Time =
    let time = faker.Date.Future(10, DateTime.Now)
    { Hour = time.Hour
      Minute = time.Minute }

let private generateTimeSoon () : DateTimeCustom.Time =
    let time = faker.Date.Soon(10, DateTime.Now)
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

let generateEvent () : Models.EventWriteModel =
    let start = DateTime.Now.AddDays(-1)
    { Title = faker.Company.CompanyName()
      Description = faker.Lorem.Paragraph()
      Location = faker.Address.City()
      OrganizerName = $"{faker.Person.FirstName} {faker.Person.LastName}"
      OrganizerEmail = faker.Person.Email
      MaxParticipants =
          if faker.Hacker.Random.Bool() then
              None
          else
              Some <| faker.Random.Number(1, 100)
      StartDate = DateTimeCustom.toCustomDateTime start.Date start.TimeOfDay
      EndDate = generateDateTimeCustomFuture ()
      OpenForRegistrationTime = (DateTimeOffset(start).AddDays(-1).ToUnixTimeMilliseconds().ToString())
      CloseRegistrationTime =
          if faker.Hacker.Random.Bool() then
              None
          else
              Some(
                  (DateTimeOffset(faker.Date.Future(10, refDate = start).Date)
                      .ToUnixTimeMilliseconds())
                      .ToString()
              )
      ParticipantQuestions =
          [ 0 .. faker.Random.Number(0, 5) ]
          |> List.map (fun _ -> faker.Lorem.Sentence())
      ViewUrl =
          if faker.Random.Number(0, 5) <> 0 then
              None
          else
              Some(faker.Lorem.Word())
      EditUrlTemplate = "{eventId}{editToken}"
      HasWaitingList = faker.Hacker.Random.Bool()
      IsExternal = faker.Hacker.Random.Bool()
      IsHidden =
          if faker.Random.Number(0, 10) = 0 then
              true
          else
              false
      Shortname =
          if faker.Random.Number(0, 5) <> 0 then
              None
          else
              Some(faker.Lorem.Paragraph()[0..99])
      CustomHexColor =
          if faker.Random.Number(0, 5) <> 0 then
              None
          else
              Some(faker.Random.Hexadecimal(6)[2..]) }

let generateEmail () = faker.Internet.Email()

let generateRandomString () = faker.Lorem.Paragraph()[0..199]

let generateParticipant (number_of_questions: int): Models.ParticipantWriteModel =
    { Name = $"{faker.Name.FirstName()} {faker.Name.LastName()}"
      ParticipantAnswers =
          [0..number_of_questions]
          |> List.map (fun _ -> faker.Lorem.Sentence())
      CancelUrlTemplate = "{eventId}{email}{cancellationToken}" }
