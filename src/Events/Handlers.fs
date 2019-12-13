namespace ArrangementService.Events

open Giraffe

open ArrangementService.Http
open ArrangementService.Operators
open ArrangementService.Repo
open Models

module Handlers =

    //let models = Models.models

    let getEvents =
        Service.getEvents
        >> Seq.map models.domainToView
        >> Ok

    //    let getEventsForEmployee employeeId =
    //        Service.getEventsForEmployee employeeId
    //        >> Seq.map models.domainToView
    //        >> Ok

    let getEvent = Service.getEvent

    let deleteEvent id = Service.deleteEvent id >>= sideEffect commitTransaction

    let updateEvent id =
        getBody<WriteModel>
        >> Result.map (models.writeToDomain id)
        >>= Service.updateEvent id
        >>= sideEffect commitTransaction
        >> Result.map models.domainToView

    let createEvent =
        getBody<WriteModel>
        >>= Service.createEvent
        >> Result.map models.domainToView

    let routes: HttpHandler =
        choose
            [ GET >=> choose
                          [ route "/events" >=> handle getEvents
                            routef "/events/%O" (handle << getEvent) ]
              //                            routef "/events/employee/%i" (handle << getEventsForEmployee) ]
              DELETE >=> choose [ routef "/events/%O" (handle << deleteEvent) ]
              PUT >=> choose [ routef "/events/%O" (handle << updateEvent) ]
              POST >=> choose [ route "/events" >=> handle createEvent ] ]
