module Tests.RegisterToEvent

open Expecto

open TestUtils
open Api

let tests =
    testList "Register to event" [
        test "External can join external event" {
            let event = TestData.createEvent (fun ev -> { ev with IsExternal = true; MaxParticipants = Some 1 })
            Expect.expectApiSuccess
                (fun () -> Participant.create WithoutToken.request event id id)
                "Unauthenticated users was unable to join external event"
        }

        test "External can not join internal event" {
            let event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = Some 1 })
            Expect.expectApiError
                (fun () -> Participant.create WithoutToken.request event id id)
                "Du må enten være innlogget eller arrangementet må være eksternt for at du skal få tilgang"
                "Unexpected error"
        }

        test "Internal can join external event" {
            let event = TestData.createEvent (fun ev -> { ev with IsExternal = true; MaxParticipants = Some 1 })
            Expect.expectApiSuccess
                (fun () -> Participant.create UsingJwtToken.request event id id)
                "Authenticated user couldn't join external event"
        }

        test "Internal can join internal event" {
            let event = TestData.createEvent (fun ev -> { ev with IsExternal = false; MaxParticipants = Some 1 })
            Expect.expectApiSuccess
                (fun () -> Participant.create UsingJwtToken.request event id id)
                "Authenticated user couldn't join internal event"
        }

        test "No-one can join cancelled event" {
            let event = TestData.createEvent (fun ev -> { ev with IsExternal = true })
            justEffect <| Events.cancel UsingJwtToken.request event
            Expect.expectApiError
                (fun () -> Participant.create WithoutToken.request event id id)
                "Arrangementet er kansellert"
                "Unexpected error"
            Expect.expectApiError
                (fun () -> Participant.create UsingJwtToken.request event id id)
                "Arrangementet er kansellert"
                "Unexpected error"
        }

        test "No-one can join event in the past" {
            let event = TestData.createEvent (fun ev -> { ev with EndDate = Generator.generateDateTimeCustomPast(); IsExternal = true; MaxParticipants = Some 1 })
            Expect.expectApiError
                (fun () -> Participant.create UsingJwtToken.request event id id)
                "Arrangementet tok sted i fortiden"
                "Unexpected exception"
            Expect.expectApiError
                (fun () -> Participant.create WithoutToken.request event id id)
                "Arrangementet tok sted i fortiden"
                "Unexpected exception"
        }

        test "No-one can join full events" {
            let event = TestData.createEvent (fun ev -> { ev with MaxParticipants = Some 0; HasWaitingList = false; IsExternal = true })
            Expect.expectApiError
                (fun () -> Participant.create UsingJwtToken.request event id id)
                "Arrangementet har ikke plass"
                "Unexpected exception"
            Expect.expectApiError
                (fun () -> Participant.create WithoutToken.request event id id)
                "Arrangementet har ikke plass"
                "Unexpected exception"
        }

        test "Anyone can join full events if they have a waitlist" {
            let event = TestData.createEvent (fun ev -> { ev with MaxParticipants = Some 0; HasWaitingList = true; IsExternal = true })
            Expect.expectApiSuccess
                (fun () -> Participant.create UsingJwtToken.request event id id)
                "Authenticated user couldn't join event"
            Expect.expectApiSuccess
                (fun () -> Participant.create WithoutToken.request event id id)
                "UnAuthenticated user couldn't join event"
        }
    ]
