module Email.Service

open System
open Giraffe
open System.Text
open FSharp.Data
open Newtonsoft.Json
open FifteenBelow.Json
open Microsoft.AspNetCore.Http
open System.Collections.Generic
open Newtonsoft.Json.Serialization

open Config
open Models
open CalendarInvite
open SendgridApiModels

let private sendMailProd (options: SendgridOptions) (jsonBody: string) =
    let byteBody = UTF8Encoding().GetBytes(jsonBody)
    async {
        try
            let! _ = Http.AsyncRequestString
                         (options.SendgridUrl, httpMethod = "POST",
                          headers =
                              [ "Authorization",
                                $"Bearer {options.ApiKey}"
                                "Content-Type", "application/json" ],
                          body = BinaryUpload byteBody)
            ()
        with e ->
            printfn "%A" e
            ()
    }
    |> Async.Start

type DevEmail = {
    Email : Email
    Serialized: string
    NoReplyEmail: string
}

let getDevMailbox, addDevMail, emptyDevMailbox =
    let mbLock = obj()
    let mutable mailbox = []
    let get () =
        mailbox
    let add (mail: DevEmail) =
        lock mbLock (fun () -> mailbox <- mail :: mailbox)
    let empty () =
        lock mbLock (fun () -> mailbox <- [])
    get, add, empty

let sendMail (email: Email) (context: HttpContext) =
    let sendgridConfig = context.GetService<SendgridOptions>()
    let appConfig = context.GetService<AppConfig>()

    let serializerSettings =
        let converters =
            [ OptionConverter() :> JsonConverter
              TupleConverter() :> JsonConverter
              ListConverter() :> JsonConverter
              MapConverter() :> JsonConverter
              BoxedMapConverter() :> JsonConverter
              UnionConverter() :> JsonConverter ]
            |> List.toArray :> IList<JsonConverter>

        let settings = JsonSerializerSettings()
        settings.ContractResolver <-
            CamelCasePropertyNamesContractResolver()
        settings.NullValueHandling <- NullValueHandling.Ignore
        settings.Converters <- converters
        settings

    let serializedEmail =
        (emailToSendgridFormat email appConfig.noReplyEmail,
         serializerSettings) |> JsonConvert.SerializeObject

    let actuallySendMail() =
        serializedEmail |> sendMailProd sendgridConfig

    if appConfig.isProd then
        actuallySendMail()
    else
        addDevMail { Email = email; Serialized = serializedEmail; NoReplyEmail = appConfig.noReplyEmail }
        let sendgridEnabled = not (String.IsNullOrWhiteSpace sendgridConfig.ApiKey)
        let emailWhitelisted = appConfig.sendMailInDevEnvWhiteList |> List.contains email.To
        if sendgridEnabled && emailWhitelisted then actuallySendMail ()
        ()

let private createdEventMessage viewUrl editUrl (event: Models.Event) =
    [ $"Hei {event.OrganizerName}! 😄"
      $"Arrangementet ditt {event.Title} er nå opprettet."
      ""
      $"Se arrangementet, få oversikt over påmeldte deltagere og gjør eventuelle endringer her: {viewUrl}."
      ""
      $"Her er en unik lenke for å endre arrangementet: {editUrl}."
      "Del denne kun med personer som du ønsker skal ha redigeringstilgang.🕵️" ]
    |> String.concat "<br>"

let organizerAsParticipant (event: Models.Event): Participant =
    {
      Name = event.OrganizerName
      Email = event.OrganizerEmail
      EventId = event.Id
      Department = None
      RegistrationTime = DateTimeOffset.Now.ToUnixTimeSeconds()
      CancellationToken = Guid.Empty
      EmployeeId = Some event.OrganizerId
    }

let private createEmail viewUrl editUrl noReplyMail (event: Models.Event) =
    let message = createdEventMessage viewUrl editUrl event
    { Subject = $"Du opprettet {event.Title}"
      Message = message
      To = event.OrganizerEmail
      CalendarInvite =
          createCalendarAttachment
              (event, organizerAsParticipant event, noReplyMail, message, Create) |> Some
    }

let sendNewlyCreatedEventMail viewUrl editUrl (event: Models.Event) (ctx: HttpContext) =
    let config = ctx.GetService<AppConfig>()
    let mail =
        createEmail viewUrl editUrl config.noReplyEmail event
    sendMail mail ctx

let private getQuestionsAndAnswers title (questionAndAnswer: QuestionAndAnswer list) =
    [
        if not (List.isEmpty questionAndAnswer) then
            ""
            $"{title}:"
            ""
            yield! List.map (fun qa -> $"""{qa.Question} <br>- {qa.Answer}<br>""") questionAndAnswer
            ""
        else ""
    ]

let private inviteMessage viewUrl cancelUrl (event: Models.Event) (questionAndAnswers: QuestionAndAnswer list) =
    [ "Hei! 😄"
      ""
      $"Du er nå påmeldt <a href=\"{viewUrl}\">{event.Title}</a>."
      $"Vi gleder oss til å se deg på {event.Location} den {DateTimeCustom.toReadableString (DateTimeCustom.toCustomDateTime event.StartDate event.StartTime)} 🎉"

      yield! getQuestionsAndAnswers "Dine svar" questionAndAnswers

      if event.MaxParticipants.IsSome then
        "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger<br>kan delta. Da blir det plass til andre på ventelisten 😊"
      else "Gjerne meld deg av dersom du ikke lenger har mulighet til å delta."
      $"Du kan melde deg av <a href=\"{cancelUrl}\">via denne lenken</a>."
      ""
      $"Bare send meg en mail på <a href=\"mailto:{event.OrganizerEmail}\">{event.OrganizerEmail}</a> om det er noe du lurer på."
      "Vi sees!"
      ""
      $"Hilsen {event.OrganizerName} i Bekk" ]
    |> String.concat "<br>" // Sendgrid formats to HTML, \n does not work

let private waitlistedMessage viewUrl cancelUrl (event: Models.Event) (questionAndAnswers: QuestionAndAnswer list) =
    [ "Hei! 😄"
      ""
      $"Du er nå på venteliste for <a href=\"{viewUrl}\">{event.Title}</a> på {event.Location} den {DateTimeCustom.toReadableString (DateTimeCustom.toCustomDateTime event.StartDate event.StartTime)}."
      "Du vil få beskjed på e-post om du rykker opp fra ventelisten."

      yield! getQuestionsAndAnswers "Dine svar" questionAndAnswers

      "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
      "kan delta. Da blir det plass til andre på ventelisten 😊"
      $"Du kan melde deg av <a href=\"{cancelUrl}\">via denne lenken</a>."
      "NB! Ta vare på lenken til senere - om du rykker opp fra ventelisten bruker du fortsatt denne til å melde deg av."
      ""
      $"Bare send meg en mail på <a href=\"mailto:{event.OrganizerEmail}\">{event.OrganizerEmail}</a> om det er noe du lurer på."
      "Vi sees!"
      ""
      $"Hilsen {event.OrganizerName} i Bekk" ]
    |> String.concat "<br>"

let createNewParticipantMail
    viewUrl
    cancelUrl
    (event: Models.Event)
    isWaitlisted
    noReplyMail
    (participant: Participant)
    (questionAndAnswers: QuestionAndAnswer list)
    =
    let message =
        if isWaitlisted
        then waitlistedMessage viewUrl cancelUrl event questionAndAnswers
        else inviteMessage viewUrl cancelUrl event questionAndAnswers

    { Subject = event.Title
      Message = message
      To = participant.Email
      CalendarInvite =
          createCalendarAttachment
              (event, participant, noReplyMail, message, Create) |> Some
    }

let private createCancelledParticipationMailToOrganizer
    (event: Models.Event)
    participant
    (participantAnswers: QuestionAndAnswer list)
    =
        let message =
            [ $"{participant.Name} har meldt seg av {event.Title}"

              yield! getQuestionsAndAnswers "Deltaker har svart" participantAnswers
            ]
            |> String.concat "<br>"

        { Subject = "Avmelding"
          Message = message
          To = event.OrganizerEmail
          CalendarInvite = None
        }

let private createCancelledParticipationMailToAttendee
    (event: Models.Event)
    (participant: Models.Participant)
    =
    { Subject = "Avmelding"
      Message = [
                $"Vi bekrefter at du nå er avmeldt {event.Title}."
                ""
                "Takk for at du gir beskjed! Vi håper å se deg ved en senere anledning.😊"
                ]
                |> String.concat "<br>"
      To = participant.Email
      CalendarInvite = None
    }

let createFreeSpotAvailableMail
    (event: Models.Event)
    (participant: Models.Participant)
    =
    { Subject = $"Du har fått plass på {event.Title}!"
      Message = $"Du har rykket opp fra ventelisten for {event.Title}! Hvis du ikke lenger kan delta, meld deg av med lenken fra forrige e-post."
      To = participant.Email
      CalendarInvite = None
    }

let private createCancelledEventMail
    (message: string)
    (event: Models.Event)
    noReplyMail
    (participant: Participant)
    =
    { Subject = $"Avlyst: {event.Title}"
      Message = message
      To = participant.Email
      CalendarInvite =
          createCalendarAttachment
              (event, participant, noReplyMail, message, Cancel) |> Some
    }

let private sendMailToFirstPersonOnWaitingList
    (event: Models.Event)
    (personWhoGotIt: Models.Participant)
    context
    =
    sendMail (createFreeSpotAvailableMail event personWhoGotIt) context

let private sendMailToOrganizerAboutCancellation event participant participantAnswers context =
    let mail = createCancelledParticipationMailToOrganizer event participant participantAnswers
    sendMail mail context

let private sendMailWithCancellationConfirmation event participant context =
    let mail = createCancelledParticipationMailToAttendee event participant
    sendMail mail context

let sendParticipantCancelMails (event: Models.Event) (participant: Models.Participant) (participantAnswers: QuestionAndAnswer list) (personWhoGotIt: ParticipantAndAnswers option) context =
    sendMailToOrganizerAboutCancellation event participant participantAnswers context
    sendMailWithCancellationConfirmation event participant context
    match personWhoGotIt with
    | Some person -> sendMailToFirstPersonOnWaitingList event person.Participant context
    | None -> ()

let private createCancellationConfirmationToOrganizer
    (event: Models.Event)
    (messageToParticipants: string)
    =
    { Subject = $"Avlyst: {event.Title}"
      Message = [
                $"Du har avlyst arrangementet ditt {event.Title}."
                "Denne meldingen ble sendt til alle påmeldte:"
                ""
                messageToParticipants
                ]
                |> String.concat "<br>"
      To = event.OrganizerEmail
      CalendarInvite = None
    }

let sendCancellationMailToParticipants
    (messageToParticipants: string)
    noReplyMail
    participants
    event
    ctx
    =
    let messageToParticipants =
        messageToParticipants.Replace("\n", "<br>")[1..(messageToParticipants.Length-2)]

    let sendMailToParticipant participant =
        sendMail
            (createCancelledEventMail messageToParticipants event
                 noReplyMail participant) ctx

    sendMail
            (createCancellationConfirmationToOrganizer event messageToParticipants) ctx

    participants |> Seq.iter sendMailToParticipant

    ()
