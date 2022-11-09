module Utils

open System
open System.Text.RegularExpressions

let matches (re: string) (str: string) =
    Regex(re).IsMatch(str)

module Option =
    let fromString (x: string) =
        if String.IsNullOrWhiteSpace(x)
        then None
        else Some x
