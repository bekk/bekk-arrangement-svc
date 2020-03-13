namespace ArrangementService.Participant

open ArrangementService

open ResultComputationExpression
open ArrangementService.Email
open CalendarInvite
open Queries
open UserMessages
open Models
open ArrangementService.DomainModels
open DateTime
open Microsoft.AspNetCore.Http
open Giraffe
open ArrangementService.Config

module Service =

    let repo = Repo.from models

    let private inviteMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          sprintf "Du er nå påmeldt %s." event.Title.Unwrap
          sprintf "Vi gleder oss til å se deg på %s den %s 🎉"
              event.Location.Unwrap (toReadableString event.StartDate)
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Meld deg av her: %s." redirectUrl
          "Bare spør meg om det er noe du lurer på."
          "Vi sees!"
          sprintf "Hilsen %s i Bekk" event.OrganizerEmail.Unwrap ]
        |> String.concat "<br>" // Sendgrid formats to HTML, \n does not work

    let createNewParticipantMail
        createCancelUrl
        (event: Event)
        (participant: Participant)
        =
        let message = inviteMessage (createCancelUrl participant) event
        { Subject = event.Title.Unwrap
          Message = message
          From = event.OrganizerEmail
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant.Email, message, Create) |> Some }

    let private createCancelledParticipationMail
        (event: Event)
        (participant: Participant)
        fromMail
        =
        { Subject = "Avmelding"
          Message =
              sprintf "%s har meldt seg av %s" participant.Name.Unwrap
                  event.Title.Unwrap
          From = fromMail
          To = event.OrganizerEmail
          CalendarInvite = None }

    let private createFreeSpotAvailableMail
        (event: Event)
        (participant: Participant)
        =
        { Subject = sprintf "Ledig plass på %s" event.Title.Unwrap
          Message =
              "Du har automatisk fått plass, blabla, gå til link for å melde deg av"
          From = event.OrganizerEmail // kanskje noreply?
          To = participant.Email
          CalendarInvite = None }

    let private createCancelledEventMail
        (message: string)
        (event: Event)
        (participant: Participant)
        =
        { Subject = sprintf "Avlyst: %s" event.Title.Unwrap
          Message = message.Replace("\n", "<br>")
          From = event.OrganizerEmail
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant.Email, message, Cancel) |> Some }

    let registerParticipant createMail registration =
        result {

            for participant in repo.create registration do

                yield Service.sendMail (createMail participant)
                return participant
        }

    let getParticipant (eventId, email) =
        result {
            for participants in repo.read do

                let! participant = participants
                                   |> queryParticipantByKey (eventId, email)

                return participant
        }

    let getParticipantsForEvent (eventId: Event.Id) =
        result {
            for participants in repo.read do

                let attendees = participants |> queryParticipantsBy eventId

                return Seq.map models.dbToDomain attendees
        }

    let getParticipationsForParticipant email =
        result {
            for participants in repo.read do
                let participantsByMail =
                    participants |> queryParticipantionByParticipant email

                return Seq.map models.dbToDomain participantsByMail
        }

    let private sendMailToFirstPersonOnWaitingList
        (event: Event)
        (participants: Participant seq)
        (context: HttpContext)
        =
        // let hasWaitingList = event.hasWaitingList
        let hasWaitingList = true // ^ Must implement

        if hasWaitingList then

            let waitingList =
                participants
                |> Seq.sortBy (fun participant -> participant.RegistrationTime)
                |> Seq.skip event.MaxParticipants.Unwrap

            let personWhoGotIt = Seq.tryHead waitingList

            personWhoGotIt
            |> function
            | Some participant ->
                Service.sendMail
                    (createFreeSpotAvailableMail event participant) context
            | None -> ()

    let private sendMailToOrganizerAboutCancellation event participant context =
        let config = getConfig context
        let mail =
            createCancelledParticipationMail event
                (models.dbToDomain participant)
                (EmailAddress config.noReplyEmail)
        Service.sendMail mail context

    let deleteParticipant (event, email) =
        result {
            for participant in getParticipant (event.Id, email) do
                for participants in getParticipantsForEvent (event.Id) do

                    let guyHadASpotAndWasNotOnWaitingList = true

                    if guyHadASpotAndWasNotOnWaitingList then
                        yield sendMailToOrganizerAboutCancellation event
                                  participant
                        let eventHasWaitingList = true // event.hasWitingList
                        if eventHasWaitingList then
                            yield sendMailToFirstPersonOnWaitingList event
                                      participants

                    repo.del participant

                    return participationSuccessfullyDeleted (event.Id, email)
        }

    let sendCancellationMailToParticipants
        messageToParticipants
        participants
        event
        ctx
        =
        let sendMailToParticipant participant =
            Service.sendMail
                (createCancelledEventMail messageToParticipants event
                     participant) ctx

        participants |> Seq.iter sendMailToParticipant

        Ok()
