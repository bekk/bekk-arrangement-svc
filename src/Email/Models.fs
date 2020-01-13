namespace ArrangementService.Email

open ArrangementService
open SendgridApiModels
open ArrangementService.DomainModels

module Models =
    let emailToSendgridFormat (email: Email): SendGridFormat =
        { Personalizations =
              [ { To = [ { Email = email.To.Unwrap } ]
                  Cc = [ { Email = email.Cc.Unwrap } ] } ]
          From = { Email = email.From.Unwrap }
          Subject = email.Subject
          Content =
              [ { Value = email.Message
                  Type = "text/html" } ]
          Attachments =
              [ { Content =
                      email.CalendarInvite
                      |> System.Text.Encoding.UTF8.GetBytes
                      |> System.Convert.ToBase64String
                  Type = "text/calendar; method=REQUEST"
                  Filename = sprintf "%s.ics" email.Subject } ] }