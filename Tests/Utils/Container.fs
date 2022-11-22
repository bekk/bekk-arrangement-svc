module rec Container

open System
open Fli

open Utils

let ContainerName =
    Environment.GetEnvironmentVariable("ARRANGEMENT_SVC_TESTCONTAINER")
    |> Option.ofObj
    |> Option.defaultValue "Arrangement_Svc_TestContainer"

let ContainerManagerProgram =
    Environment.GetEnvironmentVariable("ARRANGEMENT_SVC_CONTAINER_MANAGER")
    |> Option.ofObj
    |> Option.defaultValue "podman"

let runContainer () =
    cli{
        Exec ContainerManagerProgram
        Arguments $"""run --name {ContainerName} -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=<YourStrong!Passw0rd>" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2017-latest"""
    }
    |> Command.execute
    |> ignore
    ()

let startContainer () =
    cli{
        Exec ContainerManagerProgram
        Arguments $"start {ContainerName}"
    }
    |> Command.execute
    |> ignore
    ()

let containerExists () =
    printfn "Container exists?"
    let result =
        cli {
            Exec ContainerManagerProgram
            Arguments $"""ps --all --filter name={ContainerName} --format "{{.ID}}" """
        }
        |> Command.execute
    printfn "Result is: %A" result
    match result.Text with
    | None -> false
    | Some text ->
        printfn "Text is: %A" text
        printfn "Is it empty? %A" (text <> "")
        text <> ""

let containerIsStopped() =
    let result =
        cli {
            Exec ContainerManagerProgram
            Arguments $"""ps --all --filter name={ContainerName} --format "{{.Status}}"""
        }
        |> Command.execute
    match result.Text with
    | None -> failwith "Error when getting container info"
    | Some text ->
        text.Contains "Exited"

let waitForContainer() =
     cli {
         Exec ContainerManagerProgram
         Arguments $"wait --condition running {ContainerName}"
     }
     |> Command.execute
     |> ignore

let containerIsRunning() =
    let result =
        cli {
            Exec ContainerManagerProgram
            Arguments $"""ps --all --filter name={ContainerName} --format "{{.Status}}"""
        }
        |> Command.execute
    match result.Text with
    | None -> failwith "Error when getting container info"
    | Some text ->
        text.Contains "Up"
