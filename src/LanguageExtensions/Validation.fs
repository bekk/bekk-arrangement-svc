module Validation

let apply funcResult rightResult =
    match funcResult, rightResult with
    | Error vs, Error es -> List.concat [ vs; es ] |> Error
    | Error vs, Ok _ -> Error vs
    | Ok _, Error es -> Error es
    | Ok f, Ok v -> f v |> Ok

let (<*>) = apply

let validate f errorMessage (input: 'Type) =
    if f input then Ok() else Error errorMessage

let sequence x =
    x
    |> List.fold (fun acc ->
        function
        | Ok() -> acc
        | Error e -> acc @ [ e ]) []

let validateAll constructor thingToValidate validationFunctions =
    validationFunctions
    |> List.map
        (fun validationFunction -> validationFunction thingToValidate)
    |> sequence
    |> function
    | [] -> constructor thingToValidate |> Ok
    | errors -> Error errors

let optionally f x =
    match x with
    | None -> Ok ()
    | Some x -> f x

let rec every f list =
    match list with
    | [] -> Ok ()
    | x :: xs ->
        match f x with
        | Error e -> Error e
        | Ok _ -> every f xs
