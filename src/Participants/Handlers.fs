namespace ArrangementService.Participants

open Giraffe

open ArrangementService.Http
open ArrangementService.Operators
open ArrangementService.Repo
open Models

module Handlers =

    let registerForEvent (email, id) =
        getBody<WriteModel>
        >> Result.map (models.writeToDomain (id, email))
        >>= Service.registerParticipant
        >> Result.map models.domainToView

    let getParticipants =
        Service.getParticipants
        >> Seq.map models.domainToView
        >> Ok

    let getParticipantEvents email =
        Service.getParticipantEvents email
        >> Result.map (Seq.map models.domainToView)
        >> Ok

    let deleteParticipant (email, id) =
        Service.deleteParticipant email id >>= sideEffect commitTransaction

    let routes: HttpHandler =
        choose
            [ GET >=> choose
                          [ route "/participants" >=> handle getParticipants
                            routef "/participant/%s" (handle << getParticipantEvents) ]
              DELETE
              >=> choose [ routef "/participant/%s/events/%O" (handle << deleteParticipant) ]
              POST >=> choose [ routef "/participant/%s/events/%O" (handle << registerForEvent) ] ]
