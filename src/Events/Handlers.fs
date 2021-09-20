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

open FSharp.Control.Tasks.V2

module Handlers =

    type RemoveEvent = 
        | Cancel
        | Delete

    let getEvents: AsyncHandler<ViewModel list> =
        taskResult {
            let! events = Service.getEvents
            return Seq.map domainToView events |> Seq.toList
        }

    let getPastEvents: Handler<ViewModel list> =
        result {
            let! events = Service.getPastEvents
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


            let! result =  match removeEventType with 
                            | Cancel -> Service.cancelEvent event
                            | Delete -> Service.deleteEvent (Id id)
            
            yield Service.sendCancellationMailToParticipants
                      messageToParticipants (EmailAddress config.noReplyEmail) participants.attendees event

            return result
        }

    let getEmployeeId = Auth.getUserId // option int
                            >> Option.map Event.EmployeeId  // option EmployeeId
                            >> Option.withError [UserMessages.couldNotRetrieveUserId] // Result<EmployeeId, UserMessage list>

    let updateEvent (id:Key) =
        result {
            let! writeModel = getBody<WriteModel>
            let! updatedEvent = Service.updateEvent (Id id) writeModel
            return domainToView updatedEvent
        }

    let createEvent =
        result {
            let! writeModel = getBody<WriteModel>

            let redirectUrlTemplate =
                HttpUtility.UrlDecode writeModel.editUrlTemplate

            let viewUrl = writeModel.viewUrl
            let createEditUrl (event: Event) =
                redirectUrlTemplate.Replace("{eventId}",
                                            event.Id.Unwrap.ToString())
                                   .Replace("{editToken}",
                                            event.EditToken.ToString())

            let! employeeId = getEmployeeId

            let! newEvent = Service.createEvent viewUrl createEditUrl employeeId.Unwrap writeModel

            return domainToViewWithEditInfo newEvent
        }

    let deleteEvent = deleteOrCancelEvent Delete
    let cancelEvent = deleteOrCancelEvent Cancel


    let getEventAndParticipationSummaryForEmployee employeeId = 
        result {
            let! events = Service.getEventsOrganizedByOrganizerId (Event.EmployeeId employeeId)
            let! participations = Service.getParticipationsByEmployeeId (Event.EmployeeId employeeId)
            return Participant.Models.domainToLocalStorageView events participations
        }

    let getEventIdByShortname =
        result {
            let! shortnameEncoded = queryParam "shortname"
            let shortname = System.Web.HttpUtility.UrlDecode(shortnameEncoded)
            let! event = Service.getEventByShortname shortname
            return event.Id.Unwrap
        }

    let routes: HttpHandler =
        choose
            [ GET_HEAD
              >=> choose
                      [ route "/events" >=>
                            checkAsync (isAuthenticated >> fun x -> task {return x})
                            >=> handleAsync getEvents
                            |> withTransactionAsync

                        route "/events/previous" >=>
                            check isAuthenticated
                            >=> handle getPastEvents
                            |> withTransaction

                        routef "/events/%O" (fun eventId -> 
                            check (eventIsExternalOrUserIsAuthenticated eventId)
                            >=> (handle << getEvent) eventId
                            |> withTransaction)

                        routef "/events/organizer/%s" (fun email -> 
                            check isAuthenticated
                            >=> (handle << getEventsOrganizedBy) email
                            |> withTransaction)

                        routef "/events-and-participations/%i" (fun id ->
                            check (isAdminOrAuthenticatedAsUser id)
                            >=> (handle << getEventAndParticipationSummaryForEmployee) id
                            |> withTransaction) 
                        
                        route "/events/id" >=> handle getEventIdByShortname |> withTransaction
                      ]
              DELETE
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            check (userCanEditEvent id)
                            >=> (handle << cancelEvent) id
                            |> withTransaction)
                        routef "/events/%O/delete" (fun id -> 
                            check (userCanEditEvent id)
                            >=> (handle << deleteEvent) id
                            |> withTransaction)
                        ]
              PUT
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            check (userCanEditEvent id)
                            >=> (handle << updateEvent) id
                            |> withTransaction) ]
              POST 
              >=> choose 
                    [ route "/events" >=>
                            check isAuthenticated
                            >=> handle createEvent 
                            |> withTransaction] ]
