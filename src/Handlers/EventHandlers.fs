module Event.Handlers

open Auth
open Giraffe
open System.Web

open Http
open Config
open Email.Types
open Event.Types
open Event.Models
open ResultComputationExpression

type RemoveEvent = 
    | Cancel
    | Delete

let getEvents: Handler<ViewModel list> =
    result {
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

    
let deleteOrCancelEvent (removeEventType:RemoveEvent) id: Handler<string> =
    result {
        let! messageToParticipants = getBody<string> ()
        let! event = Service.getEvent (Id id)
        let! participants = Service.getParticipantsForEvent event

        let! config = getConfig >> Ok >> Task.wrap

        let! result =  match removeEventType with 
                        | Cancel -> Service.cancelEvent event
                        | Delete -> Service.deleteEvent (Id id)
        
        yield Service.sendCancellationMailToParticipants
                  messageToParticipants (EmailAddress config.noReplyEmail) participants.attendees event
                  
        return result
    }

let getEmployeeId = 
    result {
        let! userId = getUserId 

        return! userId
                |> Option.map EmployeeId  // option EmployeeId
                |> Option.withError [UserMessages.Events.couldNotRetrieveUserId] // Result<EmployeeId, UserMessage list>
                |> Task.wrap
    }

let updateEvent (id:Key) =
    result {
        let! writeModel = getBody<WriteModel> ()
        let! updatedEvent = Service.updateEvent (Id id) writeModel
        return domainToView updatedEvent
    }

let createEvent =
    result {
        let! writeModel = getBody<WriteModel> ()

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
        let! events = Service.getEventsOrganizedByOrganizerId (EmployeeId employeeId)
        let! participations = Service.getParticipationsByEmployeeId (EmployeeId employeeId)
        return Participant.Models.domainToLocalStorageView events participations
    }

let getEventIdByShortname =
    result {
        let! shortnameEncoded = queryParam "shortname"
        let shortname = HttpUtility.UrlDecode(shortnameEncoded)
        let! event = Service.getEventByShortname shortname
        return event.Id.Unwrap
    }

let getUnfurl (idOrName: string) =
    let strSkip n (s: string) =
        s
        |> Seq.safeSkip n
        |> Seq.map string
        |> String.concat ""

    result {
        let! event =
            match System.Guid.TryParse (idOrName |> strSkip ("/events/" |> String.length)) with
            | true, guid ->
                Service.getEvent (Id guid)
            | false, _ ->
                // Vi må hoppe over leading slash (/)
                let name = idOrName |> strSkip 1
                Service.getEventByShortname name

        let! numberOfParticipants = Service.getNumberOfParticipantsForEvent event.Id
        
        return {| event = domainToView event; numberOfParticipants = numberOfParticipants.Unwrap |} 
    }

let routes: HttpHandler =
    choose
        [ GET_HEAD
          >=> choose
                  [ route "/events" >=>
                        (check isAuthenticated
                        >=> handle getEvents
                        |> withTransaction)

                    route "/events/previous" >=>
                        (check isAuthenticated
                        >=> handle getPastEvents
                        |> withTransaction)

                    routef "/events/%O" (fun eventId -> 
                        check (Authorization.eventIsExternalOrUserIsAuthenticated eventId)
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
                    
                    route "/events/id" >=> (handle getEventIdByShortname |> withTransaction)

                    routef "/events/%s/unfurl" (fun idOrName ->
                        (handle (getUnfurl idOrName)
                        |> withTransaction))
                  ]
          DELETE
          >=> choose
                  [ routef "/events/%O" (fun id ->
                        check (Authorization.userCanEditEvent id)
                        >=> (handle << cancelEvent) id
                        |> withTransaction)
                    routef "/events/%O/delete" (fun id -> 
                        check (Authorization.userCanEditEvent id)
                        >=> (handle << deleteEvent) id
                        |> withTransaction)
                    ]
          PUT
          >=> choose
                  [ routef "/events/%O" (fun id ->
                        check (Authorization.userCanEditEvent id)
                        >=> (handle << updateEvent) id
                        |> withTransaction) ]
          POST 
          >=> choose 
                [ route "/events" >=>
                        (check isAuthenticated
                        >=> handle createEvent 
                        |> withTransaction)] ]
