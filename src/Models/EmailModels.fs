module Email.Models

open Email.Types
open Email.SendgridApiModels

type Email =
    { Subject: string
      Message: string
      To: Types.EmailAddress
      CalendarInvite: string option }

let emailToSendgridFormat
    (email: Email)
    (fromMail: EmailAddress)
    : SendGridFormat =
    { Personalizations = [ { To = [ { Email = email.To.Unwrap } ] } ]
      From = { Email = fromMail.Unwrap }
      Subject = email.Subject
      Content =
          [ { Value = email.Message
              Type = "text/html" } ]
      Attachments =
          email.CalendarInvite
          |> Option.map (fun calendarInvite ->
              [ { Content =
                      calendarInvite
                      |> System.Text.Encoding.UTF8.GetBytes
                      |> System.Convert.ToBase64String
                  Type = "text/calendar; method=REQUEST"
                  Filename = "invite.ics" } ]) }
