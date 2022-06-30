module Tests.RegisterToEvent

open Expecto
open System.Net

open TestUtils

let tests =
    testList "Register to event" [

        test "External can join external event" {
            let event = { Generator.generateEvent() with IsExternal = true; MaxParticipants = None }
            let created = postEvent event
            let participant =
                Generator.generateParticipant (List.length event.ParticipantQuestions)
                |> toJson
            let email = Generator.generateEmail()
            let response, _ = postRequestWithBody participant $"/events/{created.event.id}/participants/{email}"
            Expect.equal response.StatusCode HttpStatusCode.OK "External can join external event"
        }

        test "External can not join internal event" {
            let event = { Generator.generateEvent() with IsExternal = false; MaxParticipants = None }
            let created = postEvent event
            let participant =
                Generator.generateParticipant (List.length event.ParticipantQuestions)
                |> toJson
            let email = Generator.generateEmail()
            let response, _ = postRequestWithBody participant $"/events/{created.event.id}/participants/{email}"
            Expect.equal response.StatusCode HttpStatusCode.Forbidden "External can not join external event"
        }
        test "Internal can join external event" {
            let event = { Generator.generateEvent() with IsExternal = true; MaxParticipants = None }
            let created = postEvent event
            let participant =
                Generator.generateParticipant (List.length event.ParticipantQuestions)
                |> toJson
            let email = Generator.generateEmail()
            let response, _ = postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token
            Expect.equal response.StatusCode HttpStatusCode.OK "Internal can join external event"
        }

        test "Internal can join internal event" {
            let event = { Generator.generateEvent() with IsExternal = false; MaxParticipants = None }
            let created = postEvent event
            let participant =
                Generator.generateParticipant (List.length event.ParticipantQuestions)
                |> toJson
            let email = Generator.generateEmail()
            let response, _ = postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token
            Expect.equal response.StatusCode HttpStatusCode.OK "Internal can not join external event"
        }

        test "No-one can join cancelled event" {
            let event = { Generator.generateEvent() with IsExternal = true}
            let created = postEvent event
            let _, _ = deleteRequestAuthenticated $"/events/{created.event.id}" token
            let bekkerResponse, bekkerContent =
                let participant =
                    Generator.generateParticipant (List.length event.ParticipantQuestions)
                    |> toJson
                let email = Generator.generateEmail()
                postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token
            let bekkerContent = decodeUserMessage bekkerContent

            let nonBekkerResponse, nonBekkerContent =
                let participant =
                    Generator.generateParticipant (List.length event.ParticipantQuestions)
                    |> toJson
                let email = Generator.generateEmail()
                postRequestWithBody participant $"/events/{created.event.id}/participants/{email}"
            let nonBekkerContent = decodeUserMessage nonBekkerContent

            Expect.equal bekkerResponse.StatusCode HttpStatusCode.BadRequest "Internal can not join cancelled event"
            Expect.equal bekkerContent.userMessage "Arrangementet er kansellert" "Event is cancelled"
            Expect.equal nonBekkerResponse.StatusCode HttpStatusCode.BadRequest "External can not join cancelled event"
            Expect.equal nonBekkerContent.userMessage "Arrangementet er kansellert" "Event is cancelled"
        }

        test "No-one can join event in the past" {
            let event = { Generator.generateEvent() with EndDate = (Generator.generateDateTimeCustomPast ()); IsExternal = true; MaxParticipants = None }
            let created = postEvent event
            let bekkerResponse, bekkerContent =
                let participant =
                    Generator.generateParticipant (List.length event.ParticipantQuestions)
                    |> toJson
                let email = Generator.generateEmail()
                postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token
            let bekkerContent = decodeUserMessage bekkerContent

            let nonBekkerResponse, nonBekkerContent =
                let participant =
                    Generator.generateParticipant (List.length event.ParticipantQuestions)
                    |> toJson
                let email = Generator.generateEmail()
                postRequestWithBody participant $"/events/{created.event.id}/participants/{email}"
            let nonBekkerContent = decodeUserMessage nonBekkerContent

            Expect.equal bekkerResponse.StatusCode HttpStatusCode.BadRequest "Internal can not join past event"
            Expect.equal bekkerContent.userMessage "Arrangementet tok sted i fortiden" "Event is cancelled"
            Expect.equal nonBekkerResponse.StatusCode HttpStatusCode.BadRequest "External can not join past event"
            Expect.equal nonBekkerContent.userMessage "Arrangementet tok sted i fortiden" "Event is cancelled"
        }

        test "No-one can join full events" {
            let event = { Generator.generateEvent() with MaxParticipants = Some 0; HasWaitingList = false; IsExternal = true }
            let created = postEvent event
            let bekkerResponse, bekkerContent =
                let participant =
                    Generator.generateParticipant (List.length event.ParticipantQuestions)
                    |> toJson
                let email = Generator.generateEmail()
                postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token
            let bekkerContent = decodeUserMessage bekkerContent

            let nonBekkerResponse, nonBekkerContent =
                let participant =
                    Generator.generateParticipant (List.length event.ParticipantQuestions)
                    |> toJson
                let email = Generator.generateEmail()
                postRequestWithBody participant $"/events/{created.event.id}/participants/{email}"
            let nonBekkerContent = decodeUserMessage nonBekkerContent

            Expect.equal bekkerContent.userMessage "Arrangementet har ikke plass" "Event is full"
            Expect.equal bekkerResponse.StatusCode HttpStatusCode.BadRequest "Internal can not join full event"
            Expect.equal nonBekkerContent.userMessage "Arrangementet har ikke plass" "Event is full"
            Expect.equal nonBekkerResponse.StatusCode HttpStatusCode.BadRequest "External can not join full event"
        }

        test "Anyone can join full events if they have a waitlist" {
            let event = { Generator.generateEvent() with MaxParticipants = Some 0; HasWaitingList = true; IsExternal = true }
            let created = postEvent event

            let bekkerResponse, _ =
                let participant =
                    Generator.generateParticipant (List.length event.ParticipantQuestions)
                    |> toJson
                let email = Generator.generateEmail()
                postRequestAuthenticatedWithBody participant $"/events/{created.event.id}/participants/{email}" token

            let nonBekkerResponse, _ =
                let participant =
                    Generator.generateParticipant (List.length event.ParticipantQuestions)
                    |> toJson
                let email = Generator.generateEmail()
                postRequestWithBody participant $"/events/{created.event.id}/participants/{email}"

            Expect.equal bekkerResponse.StatusCode HttpStatusCode.OK "Internal can not join full event"
            Expect.equal nonBekkerResponse.StatusCode HttpStatusCode.OK "External can not join full event"
        }
    ]
