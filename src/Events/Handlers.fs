namespace ArrangementService.Event

open ArrangementService

open Http
open ResultComputationExpression
open Repo
open Models
open Authorization

open Giraffe

module Handlers =

    let getEvents =
        result {
            for events in Service.getEvents do
                return Seq.map Models.domainToView events
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            for events in Service.getEventsOrganizedBy organizerEmail do
                return Seq.map domainToView events
        }

    let getEvent id =
        result {
            for event in Service.getEvent (Id id) do
                return domainToView event
        }

    let deleteEvent id =
        result {
            for result in Service.deleteEvent (Id id) do
                yield commitTransaction
                return result
        }

    let updateEvent id =
        result {
            for writeModel in getBody<WriteModel> do
                let! domainModel = writeToDomain id writeModel

                for updatedEvent in Service.updateEvent (Id id) domainModel do
                    yield commitTransaction
                    return domainToView updatedEvent
        }

    let createEvent =
        result {
            for writeModel in getBody<WriteModel> do
                for newEvent in Service.createEvent
                                    (fun id -> writeToDomain id writeModel) do

                    return domainToView newEvent
        }

    let routes: HttpHandler =
        choose
            [ GET
              >=> choose
                      [ route "/events" >=> handle getEvents
                        routef "/events/%O" (handle << getEvent)
                        routef "/events/organizer/%s"
                            (handle << getEventsOrganizedBy) ]
              DELETE
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            userCanEditEvent id >=> (handle << deleteEvent) id) ]
              PUT
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            userCanEditEvent id >=> (handle << updateEvent) id) ]
              POST >=> choose [ route "/events" >=> handle createEvent ] ]
