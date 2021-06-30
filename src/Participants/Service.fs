namespace ArrangementService.Participant

open ArrangementService

open ResultComputationExpression
open ArrangementService.Email
open ArrangementService.Event
open CalendarInvite
open UserMessages
open Models
open ArrangementService.DomainModels
open DateTime
open Http

module Service =

    let private inviteMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          ""
          $"Du er nå påmeldt {event.Title.Unwrap}."
          $"Vi gleder oss til å se deg på {event.Location.Unwrap} den {toReadableString event.StartDate} 🎉"
          ""
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          $"Du kan melde deg av <a href=\"{redirectUrl}\">via denne lenken</a>."
          ""
          $"Bare send meg en mail på <a href=\"mailto:{event.OrganizerEmail.Unwrap}\">{event.OrganizerEmail.Unwrap}</a> om det er noe du lurer på."
          "Vi sees!"
          ""
          $"Hilsen {event.OrganizerName.Unwrap} i Bekk" ]
        |> String.concat "<br>" // Sendgrid formats to HTML, \n does not work

    let private waitlistedMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          ""
          $"Du er nå på venteliste for {event.Title.Unwrap} på {event.Location.Unwrap} den {toReadableString event.StartDate}."
          "Du vil få beskjed på e-post om du rykker opp fra ventelisten."
          ""
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          $"Du kan melde deg av <a href=\"{redirectUrl}\">via denne lenken</a>."
          "NB! Ta vare på lenken til senere - om du rykker opp fra ventelisten bruker du fortsatt denne til å melde deg av."
          ""
          $"Bare send meg en mail på <a href=\"mailto:{event.OrganizerEmail.Unwrap}\">{event.OrganizerEmail.Unwrap}</a> om det er noe du lurer på."
          "Vi sees!"
          ""
          $"Hilsen {event.OrganizerName.Unwrap } i Bekk" ]
        |> String.concat "<br>"

    let createNewParticipantMail
        createCancelUrl
        (event: Event)
        isWaitlisted
        noReplyMail
        (participant: Participant)
        =
        let message =
            if isWaitlisted
            then waitlistedMessage (createCancelUrl participant) event
            else inviteMessage (createCancelUrl participant) event

        { Subject = event.Title.Unwrap
          Message = message
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant, noReplyMail, message, Create) |> Some 
        }

    let private createCancelledParticipationMail
        (event: Event)
        (participant: Participant)
        =
        { Subject = "Avmelding"
          Message = $"{participant.Name.Unwrap} har meldt seg av {event.Title.Unwrap}" 
          To = event.OrganizerEmail
          CalendarInvite = None 
        }

    let private createFreeSpotAvailableMail
        (event: Event)
        (participant: Participant)
        =
        { Subject = $"Du har fått plass på {event.Title.Unwrap}!" 
          Message = $"Du har rykket opp fra ventelisten for {event.Title.Unwrap}! Hvis du ikke lenger kan delta, meld deg av med lenken fra forrige e-post."
          To = participant.Email
          CalendarInvite = None 
        }

    let private createCancelledEventMail
        (message: string)
        (event: Event)
        noReplyMail
        (participant: Participant)
        =
        { Subject = $"Avlyst: {event.Title.Unwrap}"
          Message = message.Replace("\n", "<br>")
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant, noReplyMail, message, Cancel) |> Some 
        }

    let registerParticipant createMail participant =
        result {

            do! Queries.createParticipant participant

            yield Service.sendMail (createMail participant)
            return ()
        }

    let getParticipant (eventId, email: EmailAddress) =
        result {
            let! participant = Queries.queryParticipantByKey (eventId, email)

            return participant
        }
    
    let getParticipantsForEventPure (event:Event) (participants:seq<Participant>): ParticipantsWithWaitingList = 
            match event.MaxParticipants.Unwrap with
            // Max participants = 0 means participants = infinity 
            | 0 -> {
                attendees =
                    participants

                waitingList =
                    [] 
                }

            | maxParticipants -> { 
                attendees =
                    Seq.truncate maxParticipants
                        participants

                waitingList =
                    Seq.safeSkip maxParticipants
                        participants }

    let getParticipantsForEvent (event: Event): Handler<ParticipantsWithWaitingList> =
        result {
            //IO
            let! participantsForEvent =
                Queries.queryParticipantsByEventId event.Id
                >> Ok
            // Pure
            return getParticipantsForEventPure event participantsForEvent
        }
            
    let getParticipationsForParticipant email =
        result {
            
            let! participantsByMail = Queries.queryParticipantionByParticipant email >> Ok

            return participantsByMail
        }

    let private sendMailToFirstPersonOnWaitingList
        (event: Event)
        (waitingList: Participant seq)
        =
        result {
            let personWhoGotIt = Seq.tryHead waitingList
            match personWhoGotIt with
            | None -> return ()
            | Some participant ->
                yield Service.sendMail
                          (createFreeSpotAvailableMail event participant)
        }

    let private sendMailToOrganizerAboutCancellation event participant =
        result {
            let mail = createCancelledParticipationMail event participant

            yield Service.sendMail mail
        }

    let private sendParticipantCancelMails event email =
        result {
            let! participants = getParticipantsForEvent event

            let attendingParticipant =
                participants.attendees
                |> Seq.tryFind (fun attendee -> attendee.Email = email)

            match attendingParticipant with
            | None -> return ()
            | Some participant ->
                yield sendMailToOrganizerAboutCancellation event
                          participant
                let eventHasWaitingList = event.HasWaitingList
                if eventHasWaitingList then
                    yield sendMailToFirstPersonOnWaitingList event
                              participants.waitingList
                    return ()
        }

    let deleteParticipant (event, email) =
        result {
            yield sendParticipantCancelMails event email
            let! participant = getParticipant (event.Id, email)

            do! Queries.deleteParticipant participant

            return participationSuccessfullyDeleted (event.Id, email)
        }


    let sendCancellationMailToParticipants
        messageToParticipants
        noReplyMail
        participants
        event
        ctx
        =
        let sendMailToParticipant participant =
            Service.sendMail
                (createCancelledEventMail messageToParticipants event
                     noReplyMail participant) ctx

        participants |> Seq.iter sendMailToParticipant

        Ok()
    
    let getNumberOfParticipantsForEvent eventId =
        result {
            let! count = Queries.getNumberOfParticipantsForEvent eventId
            return NumberOfParticipants count
        }
    
    let getWaitinglistSpotPure (event:Event) email (participantsWithWaitingList:ParticipantsWithWaitingList) =
        let attendees = participantsWithWaitingList.attendees
        let waitingList = participantsWithWaitingList.waitingList

        let isParticipant =  
            Seq.append attendees waitingList 
            |> Seq.exists (fun y -> y.Email = email)

        if not isParticipant then
            Error [ participantNotFound email ]

        else
            let waitingListIndex = 
                waitingList 
                |> Seq.tryFindIndex (fun participant -> participant.Email = email)
            
            waitingListIndex 
                |> Option.map (fun index -> index + 1) 
                |> Option.defaultValue 0 
                |> Ok
    
    let getWaitinglistSpot eventId email =
        result {
            // IO
            let! event = Service.getEvent eventId
            let! participantsWithWaitingList = getParticipantsForEvent event

            // Pure
            return getWaitinglistSpotPure event email participantsWithWaitingList
        }
