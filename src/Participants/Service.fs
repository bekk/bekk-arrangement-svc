namespace ArrangementService.Participants

open ArrangementService
open ArrangementService.Operators
open ArrangementService.Email.Models
open ArrangementService.Email.Service

open Queries
open ErrorMessages

module Service =

    let models = Models.models
    let repo = Repo.from Models.models

    let createEmail participants (event: Events.Models.DomainModel) =
        { Subject = event.Title
          Message = event.Description
          From = EmailAddress event.OrganizerEmail
          To = EmailAddress participants
          Cc = EmailAddress event.OrganizerEmail }

    let sendEventEmail participants event context =
        let mail = createEmail participants event
        sendMail mail context |> ignore

    let registerParticipant (registration: Models.DomainModel) =
        repo.create (fun _ -> registration)
        >> Ok
        >>= Http.sideEffect (fun registration context ->
                Events.Service.getEvent registration.EventId context
                |> Result.map (fun event -> sendEventEmail registration.Email event context))

    let getParticipants = repo.read >> Seq.map models.dbToDomain

    let getParticipantEvents email =
        repo.read
        >> queryParticipantBy email
        >> Seq.map models.dbToDomain
        >> Ok


    let deleteParticipant email id =
        repo.read
        >> queryParticipantByKey (email, id)
        >> withError (participationNotFound email id)
        >> Result.map repo.del
        >> Result.bind (fun _ -> participationSuccessfullyDeleted email id)
