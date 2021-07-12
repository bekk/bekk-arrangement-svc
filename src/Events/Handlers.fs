namespace ArrangementService.Event

open ArrangementService

open Http
open ResultComputationExpression
open Models
open ArrangementService.DomainModels
open ArrangementService.Config
open ArrangementService.Email
open Authorization

open Microsoft.AspNetCore.Http
open Giraffe
open System.Web
open ArrangementService.Auth

module Handlers =

    type RemoveEvent = 
        | Cancel
        | Delete

    let getEvents: Handler<ViewModel list> =
        result {
            let! events = Service.getEvents
            return Seq.map domainToView events |> Seq.toList
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            let! events = Service.getEventsOrganizedBy (EmailAddress organizerEmail)
            return Seq.map domainToView events |> Seq.toList
        }

    let getEvent id =
        result {
            let! event = Service.getEvent (Id id)
            return domainToView event
        }

        
    let deleteOrCancelEvent (removeEventType:RemoveEvent) id : (HttpContext -> Result<string,UserMessage.UserMessage list>)=
        result {
            let! messageToParticipants = getBody<string>
            let! event = Service.getEvent (Id id)
            let! participants = Service.getParticipantsForEvent event

            let! config = getConfig >> Ok

            yield Service.sendCancellationMailToParticipants
                      messageToParticipants (EmailAddress config.noReplyEmail) participants.attendees event

            let! result =  match removeEventType with 
                            | Cancel -> Service.cancelEvent event
                            | Delete -> Service.deleteEvent (Id id)
            return result
        }

    let getEmployeeId = Auth.getUserId // option int
                            >> Option.map Event.EmployeeId  // option EmployeeId
                            >> Option.withError [UserMessages.couldNotRetrieveUserId] // Result<EmployeeId, UserMessage list>

    let updateEvent (id:Key) =
        result {
            let! writeModel = getBody<WriteModel>
            let! employeeId = getEmployeeId
            let! updatedEvent = Service.updateEvent employeeId (Id id) writeModel
            return domainToView updatedEvent
        }

    let createEvent =
        result {
            let! writeModel = getBody<WriteModel>

            let redirectUrlTemplate =
                HttpUtility.UrlDecode writeModel.editUrlTemplate

            let createEditUrl (event: Event) =
                redirectUrlTemplate.Replace("{eventId}",
                                            event.Id.Unwrap.ToString())
                                   .Replace("{editToken}",
                                            event.EditToken.ToString())

            let! employeeId = getEmployeeId

            let! newEvent = Service.createEvent createEditUrl employeeId.Unwrap writeModel

            return domainToViewWithEditInfo newEvent
        }

    let deleteEvent = deleteOrCancelEvent Delete
    let cancelEvent = deleteOrCancelEvent Cancel

    let routes: HttpHandler =
        choose
            [ GET
              >=> choose
                      [ route "/events" >=>
                            check (isAuthenticated)
                            >=> handle getEvents

                        routef "/events/%O" (fun eventId -> 
                            check (eventIsExternalOrUserIsAuthenticated eventId)
                            >=> (handle << getEvent) eventId)

                        routef "/events/organizer/%s" (fun email -> 
                            check isAuthenticated
                            >=> (handle << getEventsOrganizedBy) email)]
              DELETE
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            check (userCanEditEvent id)
                            >=> (handle << cancelEvent) id)
                        routef "/events/%O/delete" (fun id -> 
                            check (userCanEditEvent id)
                            >=> (handle << deleteEvent) id)
                        ]
              PUT
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            check (userCanEditEvent id)
                            >=> (handle << updateEvent) id) ]
              POST 
              >=> choose 
                    [ route "/events" >=>
                            check isAuthenticated
                            >=> handle createEvent ] ]
