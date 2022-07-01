module Tests.TestUtils

open System
open System.Text.RegularExpressions
open Expecto

module Expect =
    let throwsTC<'a, 'ex when 'ex :> exn > (f: unit -> unit) (cont: 'ex -> 'a) : 'a =
        Expect.throwsC
            f
            (fun ex -> cont (ex :?> 'ex))

let always x = fun _ -> x

let lines (str : string) : string array =
    str.Split(System.Environment.NewLine)

let contains (needle: string) (haystack: string) =
        haystack.Contains(needle)

let matches (re: string) (str: string) =
    Regex(re).IsMatch(str)

let split (delim: string) (str : string) =
    str.Split(delim)

module Seq =
    let first (xs : 'a seq) : 'a =
        xs |> Seq.take 1 |> Seq.exactlyOne

    let tryFirst (xs : 'a seq) :'a option =
        let found = xs |> Seq.take 1
        if Seq.isEmpty found
        then None
        else Some (found |> Seq.exactlyOne)

module Option =
    let fromString (x: string) =
        if String.IsNullOrWhiteSpace(x)
        then None
        else Some x
