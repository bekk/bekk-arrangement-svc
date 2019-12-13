namespace ArrangementService.Events

open ArrangementService.Operators
open ArrangementService

open Queries
open ErrorMessages

module Service =

    let models = Models.models
    let repo = Repo.from models

    let getEvents = repo.read >> Seq.map models.dbToDomain

    //    let getEventsForEmployee employeeId =
    //        repo.read
    //        >> queryEventsForEmployee employeeId
    //        >> Seq.map models.dbToDomain

    let getEvent id =
        repo.read
        >> queryEventBy id
        >> withError (eventNotFound id)
        >> Result.map models.dbToDomain

    let createEvent writemodel = repo.create (fun id -> models.writeToDomain id writemodel) >> Ok

    let updateEvent id event =
        repo.read
        >> queryEventBy id
        >> withError (eventNotFound id)
        >> Result.map (repo.update event)

    let deleteEvent id =
        repo.read
        >> queryEventBy id
        >> withError (eventNotFound id)
        >> Result.map repo.del
        >> Result.bind (fun _ -> eventSuccessfullyDeleted id)
