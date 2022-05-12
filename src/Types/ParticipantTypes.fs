module Participant.Types

open Utils
open Validation
open UserMessage

type EmployeeId = 
    | EmployeeId of int option
    member this.Unwrap =
        match this with
        | EmployeeId id -> id

type Name =
    | Name of string

    member this.Unwrap =
        match this with
        | Name name -> name

    static member Parse(name: string) =
        [ validateMinLength 3 (BadInput "Navn må ha minst 3 tegn")
          validateMaxLength 60 (BadInput "Navn kan ha maks 60 tegn") ]
        |> validateAll Name name

type ParticipantAnswers =
    | ParticipantAnswers of string list

    member this.Unwrap =
        match this with
        | ParticipantAnswers answers -> answers

    static member Parse(participantAnswers: string list) =
        [ validateMaxLength 1000
              (BadInput "Svar kan ha maks 1000 tegn")
          |> every ]
        |> validateAll ParticipantAnswers participantAnswers