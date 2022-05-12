module Option

let withError error result =
    match result with
    | Some x -> Ok x
    | None -> Error error