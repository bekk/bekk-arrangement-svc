module Email.Types

open Validation
open UserMessage

type EmailAddress =
    | EmailAddress of string

    member this.Unwrap =
        match this with
        | EmailAddress e -> e

    static member Parse(address: string) =

        let isAtSign char = char = '@'

        [ validate (String.exists isAtSign) (BadInput "E-post må inneholde en alfakrøll (@)") ]
        |> validateAll EmailAddress address
