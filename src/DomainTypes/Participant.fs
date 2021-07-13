namespace ArrangementService.Participant

open ArrangementService
open Validation
open ArrangementService.Utils
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

type Comment =
    | Comment of string

    member this.Unwrap =
        match this with
        | Comment comment -> comment

    static member Parse(comment: string) =
        [ validateMaxLength 500 (BadInput "Kommentar kan ha maks 500 tegn") ]
        |> validateAll Comment comment
