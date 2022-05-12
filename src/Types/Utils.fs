module Utils

open Validation
open UserMessage

let validateMinLength length errorMessage text =
    validate (fun x -> String.length x >= length) errorMessage text

let validateMaxLength length errorMessage text =
    validate (fun x -> String.length x <= length) errorMessage text

let validateBefore errorMessage (before, after) =
    validate (fun (x, y) -> x < y) errorMessage (before, after)

let validateNotNegative errorMessage number =
    validate (fun x -> x >= 0) errorMessage number

let validateDateRange startDate endDate =
    [ validateBefore (BadInput "Startdato må være før sluttdato") ]
    |> validateAll id (startDate, endDate)

let validateDoesNotContain (values: 'T seq) errorMessage (list: 'T seq) =
    validate (fun xs ->
                    values
                    |> Seq.forall (fun value -> xs |> Seq.contains value |> not )
    ) errorMessage list
