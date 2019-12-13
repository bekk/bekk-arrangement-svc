#r "paket:
nuget Fantomas 3.1.0
nuget Fake.Core.Target //"

open Fake.Core
open Fake.IO.Globbing.Operators
open Fantomas.FakeHelpers
open Fantomas.FormatConfig

let fantomasConfig =
    { IndentSpaceNum = 4
      PageWidth = 120
      SemicolonAtEndOfLine = false
      SpaceBeforeArgument = true
      SpaceBeforeColon = false
      SpaceAfterComma = true
      SpaceAfterSemicolon = true
      IndentOnTryWith = false
      ReorderOpenDeclaration = false
      SpaceAroundDelimiter = true
      KeepNewlineAfter = false
      MaxIfThenElseShortWidth = 40
      StrictMode = false }

Target.create "Format" (fun _ ->
    !!"*/*.fs"
    |> formatCode fantomasConfig
    |> Async.RunSynchronously
    |> printfn "Formatted files: %A")

Target.runOrList()