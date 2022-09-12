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
        if appConfig.sendMailInDevEnvWhiteList
           |> List.contains email.To then actuallySendMail()
        ()

let private createdEventMessage (viewUrl: string option) createEditUrl (event: Models.Event) =
    [ $"Hei {event.OrganizerName}! 😄"
      $"Arrangementet ditt {event.Title} er nå opprettet."
      match viewUrl with
      | None -> ""
      | Some url -> $"Se arrangmentet, få oversikt over påmeldte deltagere og gjør eventuelle endringer her: {url}."
      $"Her er en unik lenke for å endre arrangementet: {createEditUrl event}."
      "Del denne kun med personer som du ønsker skal ha redigeringstilgang.🕵️" ]
    |> String.concat "<br>"

let organizerAsParticipant (event: Models.Event): Participant =
    {
      Name = event.OrganizerName
      Email = event.OrganizerEmail
      EventId = event.Id
      RegistrationTime = System.DateTimeOffset.Now.ToUnixTimeSeconds()
      CancellationToken = System.Guid.Empty
      EmployeeId = Some event.OrganizerId
    }

let private createEmail viewUrl createEditUrl noReplyMail (event: Models.Event) =
    let message = createdEventMessage viewUrl createEditUrl event
    { Subject = $"Du opprettet {event.Title}"
      Message = message
      To = event.OrganizerEmail
      CalendarInvite =
          createCalendarAttachment
              (event, organizerAsParticipant event, noReplyMail, message, Create) |> Some
    }

let sendNewlyCreatedEventMail viewUrl createEditUrl (event: Models.Event) (ctx: HttpContext) =
    let config = ctx.GetService<AppConfig>()
    let mail =
        createEmail viewUrl createEditUrl config.noReplyEmail event
    sendMail mail ctx

let private inviteMessage redirectUrl (event: Models.Event) =
    [ "Hei! 😄"
      ""
      $"Du er nå påmeldt {event.Title}."
      $"Vi gleder oss til å se deg på {event.Location} den {DateTimeCustom.toReadableString (DateTimeCustom.toCustomDateTime event.StartDate event.StartTime)} 🎉"
      ""
      if event.MaxParticipants.IsSome then
        "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger<br>kan delta. Da blir det plass til andre på ventelisten 😊"
      else "Gjerne meld deg av dersom du ikke lenger har mulighet til å delta."
      $"Du kan melde deg av <a href=\"{redirectUrl}\">via denne lenken</a>."
      ""
      $"Bare send meg en mail på <a href=\"mailto:{event.OrganizerEmail}\">{event.OrganizerEmail}</a> om det er noe du lurer på."
      "Vi sees!"
      ""
      $"Hilsen {event.OrganizerName} i Bekk" ]
    |> String.concat "<br>" // Sendgrid formats to HTML, \n does not work

let private waitlistedMessage redirectUrl (event: Models.Event) =
    [ "Hei! 😄"
      ""
      $"Du er nå på venteliste for {event.Title} på {event.Location} den {DateTimeCustom.toReadableString (DateTimeCustom.toCustomDateTime event.StartDate event.StartTime)}."
      "Du vil få beskjed på e-post om du rykker opp fra ventelisten."
      ""
      "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
      "kan delta. Da blir det plass til andre på ventelisten 😊"
      $"Du kan melde deg av <a href=\"{redirectUrl}\">via denne lenken</a>."
      "NB! Ta vare på lenken til senere - om du rykker opp fra ventelisten bruker du fortsatt denne til å melde deg av."
      ""
      $"Bare send meg en mail på <a href=\"mailto:{event.OrganizerEmail}\">{event.OrganizerEmail}</a> om det er noe du lurer på."
      "Vi sees!"
      ""
      $"Hilsen {event.OrganizerName} i Bekk" ]
    |> String.concat "<br>"

let createNewParticipantMail
    createCancelUrl
    (event: Models.Event)
    isWaitlisted
    noReplyMail
    (participant: Participant)
    =
    let message =
        if isWaitlisted
        then waitlistedMessage (createCancelUrl participant) event
        else inviteMessage (createCancelUrl participant) event

    { Subject = event.Title
      Message = message
      To = participant.Email
      CalendarInvite =
          createCalendarAttachment
              (event, participant, noReplyMail, message, Create) |> Some
    }

let private createCancelledParticipationMailToOrganizer
    (event: Models.Event)
    (participant: Models.Participant)
    =
    { Subject = "Avmelding"
      Message = $"{participant.Name} har meldt seg av {event.Title}"
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
      Message = message.Replace("\n", "<br>")
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

let private sendMailToOrganizerAboutCancellation (event: Models.Event) participant context =
    let mail = createCancelledParticipationMailToOrganizer event participant
    sendMail mail context

let private sendMailWithCancellationConfirmation event participant context =
    let mail = createCancelledParticipationMailToAttendee event participant
    sendMail mail context

let sendParticipantCancelMails (event: Models.Event) (participant: Models.Participant) (personWhoGotIt: ParticipantAndAnswers option) context =
    sendMailToOrganizerAboutCancellation event participant context
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
    messageToParticipants
    noReplyMail
    participants
    event
    ctx
    =
    let sendMailToParticipant participant =
        sendMail
            (createCancelledEventMail messageToParticipants event
                 noReplyMail participant) ctx

    sendMail
            (createCancellationConfirmationToOrganizer event messageToParticipants) ctx

    participants |> Seq.iter sendMailToParticipant

    ()
