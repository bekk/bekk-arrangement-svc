module Email.CalendarInvite

open System

open Email.Types
open Event.Models
open Participant.Models

// ICS reference: https://tools.ietf.org/html/rfc5545
let reminderObject =
    [ "BEGIN:VALARM"
      "TRIGGER;RELATED=START:-PT30M"
      "ACTION:DISPLAY"
      "DESCRIPTION:REMINDER"
      "END:VALARM" ]
    |> String.concat "\n"

let recurringObject = "RRULE:FREQ=WEEKLY;COUNT=3;INTERVAL=1;WKST=MO\n" // Eksempel, må implementeres

let timezoneObject =
    [ "BEGIN:VTIMEZONE"
      "TZID:W. Europe Standard Time"
      "BEGIN:STANDARD"
      "DTSTART:16010101T030000"
      "TZOFFSETFROM:+0200"
      "TZOFFSETTO:+0100"
      "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=10"
      "END:STANDARD"
      "BEGIN:DAYLIGHT"
      "DTSTART:16010101T020000"
      "TZOFFSETFROM:+0100"
      "TZOFFSETTO:+0200"
      "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=3"
      "END:DAYLIGHT"
      "END:VTIMEZONE" ]
    |> String.concat "\n"

type CalendarInviteStatus =
    | Create
    | Cancel

let eventObject ((event: Event), (participant: Participant),
                 (noReplyMail: EmailAddress), (message: string),
                 (status: CalendarInviteStatus))
    =
    let utcNow =
        DateTimeCustom.toUtcString (DateTimeCustom.toCustomDateTime DateTime.UtcNow (TimeSpan()))
    [ "BEGIN:VEVENT"
      $"UID:{event.Id.Unwrap}"
      $"DTSTART;TZID=W. Europe Standard Time:{DateTimeCustom.toDateString event.StartDate}"
      $"DTEND;TZID=W. Europe Standard Time: {DateTimeCustom.toDateString event.EndDate}"
      $"DTSTAMP:{utcNow}"
      $"ORGANIZER:mailto:{noReplyMail.Unwrap}"
      $"ATTENDEE;PARTSTAT=ACCEPTED;RSVP=FALSE;CN={event.OrganizerName.Unwrap}:mailto:{event.OrganizerEmail.Unwrap}"
      $"ATTENDEE;PARTSTAT=ACCEPTED;RSVP=FALSE;CN={participant.Name.Unwrap}:mailto:{participant.Email.Unwrap}"
      $"SUMMARY;LANGUAGE=nb-NO:{event.Title.Unwrap}"
      sprintf "DESCRIPTION;LANGUAGE=nb-NO:%s" (message.Replace("<br>", "\n"))
      $"X-ALT-DESC;FMTTYPE=text/html:{event.Description.Unwrap}"
      $"LOCATION;LANGUAGE=nb-NO:{event.Location.Unwrap}"

      (if status = Create then "STATUS:CONFIRMED" else "STATUS:CANCELLED")
      (if status = Create then "SEQUENCE:0" else "SEQUENCE:1")

      reminderObject
      // if recurring, insert recurringObject. TODO: implement frontend.
      "END:VEVENT" ]
    |> String.concat "\n"

let createCalendarAttachment eventDetails =
    [ "BEGIN:VCALENDAR"
      "CALSCALE:GREGORIAN"
      "METHOD:REQUEST"
      "PRODID:-//Bekk//arrangement-svc//NO"
      "VERSION:2.0"
      timezoneObject
      eventObject eventDetails
      "END:VCALENDAR" ]
    |> String.concat "\n"
