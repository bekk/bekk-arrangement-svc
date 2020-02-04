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

module Service =

    let repo = Repo.from models

    let private inviteMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          sprintf "Du er nå påmeldt %s." event.Title.Unwrap
          sprintf "Vi gleder oss til å se deg på %s den %s 🎉"
              event.Location.Unwrap (toReadableString event.StartDate)
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Klikk her for å melde deg av: %s." redirectUrl
          "Bare spør meg om det er noe du lurer på."
          "Vi sees!"
          sprintf "Hilsen %s i Bekk" event.OrganizerEmail.Unwrap ]
        |> String.concat "\n"

    let private createEmail redirectUrl (participant: Participant) (event: Event) =
        { Subject = sprintf "Du ble påmeldt %s" event.Title.Unwrap
          Message = inviteMessage redirectUrl event
          From = event.OrganizerEmail
          To = participant.Email
          CalendarInvite = createCalendarAttachment event participant.Email }

    let private sendEventEmail redirectUrl (participant: Participant) =
        result {
            for event in Event.Service.getEvent participant.EventId do
                let mail = createEmail redirectUrl participant event
                yield Service.sendMail mail
        }

    let registerParticipant redirectUrlTemplate registration =
        result {

            for participant in repo.create registration do

                let redirectUrl =
                    createRedirectUrl redirectUrlTemplate participant
                yield sendEventEmail redirectUrl participant

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

                let attendies = participants |> queryParticipantsBy eventId

                return attendies
        }

    let getParticipationsForParticipant email =
        result {
            for participants in repo.read do
                let participantsByMail =
                    participants |> queryParticipantionByParticipant email
                return Seq.map models.dbToDomain participantsByMail
        }

    let deleteParticipant (eventId, email) =
        result {
            for participant in getParticipant (eventId, email) do

                repo.del participant

                return participationSuccessfullyDeleted (eventId, email)
        }
