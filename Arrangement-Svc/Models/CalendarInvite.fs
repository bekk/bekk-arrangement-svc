module Email.CalendarInvite

open System

open Models

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
    | Update
    | Cancel

let eventObject (event: Models.Event, participant: Participant,
                 noReplyMail: string, message: string,
                 status: CalendarInviteStatus)
    =
    let utcNow =
        DateTimeCustom.toUtcString (DateTimeCustom.toCustomDateTime DateTime.UtcNow (TimeSpan()))

    // We don't track number of edits, so this is a poor man's incrementing number.
    //
    // There is no requirement to increase the sequence number for subsequent changes,
    // but the mail clients (at least gmail and outlook) uses this to render the correct
    // information for an event, notification than an event has been updated and so on.
    //
    // Ref https://icalendar.org/iCalendar-RFC-5545/3-8-7-4-sequence-number.html
    let sequence =
        $"SEQUENCE:{DateTime.UtcNow.Ticks:yyMMddHHmmss}"

    [ "BEGIN:VEVENT"
      $"UID:{event.Id}"
      $"DTSTART;TZID=W. Europe Standard Time:{DateTimeCustom.toDateString (DateTimeCustom.toCustomDateTime event.StartDate event.StartTime)}"
      $"DTEND;TZID=W. Europe Standard Time: {DateTimeCustom.toDateString (DateTimeCustom.toCustomDateTime event.EndDate event.StartTime)}"
      $"DTSTAMP:{utcNow}"
      $"ORGANIZER:mailto:{noReplyMail}"
      $"ATTENDEE;PARTSTAT=ACCEPTED;RSVP=FALSE;CN={event.OrganizerName}:mailto:{event.OrganizerEmail}"
      $"ATTENDEE;PARTSTAT=ACCEPTED;RSVP=FALSE;CN={participant.Name}:mailto:{participant.Email}"
      $"SUMMARY;LANGUAGE=nb-NO:{event.Title}"
      sprintf "DESCRIPTION;LANGUAGE=nb-NO:%s" (message.Replace("<br>", "\n"))
      $"X-ALT-DESC;FMTTYPE=text/html:{event.Description}"
      $"LOCATION;LANGUAGE=nb-NO:{event.Location}"

      match status with
      | Create ->
          "STATUS:CONFIRMED"
          "SEQUENCE:0"
      | Update ->
          "STATUS:CONFIRMED"
          sequence
      | Cancel ->
          "STATUS:CANCELLED"
          sequence

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
