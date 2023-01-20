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


let runCLI arguments =
    cli {
        Exec ContainerManagerProgram
        Arguments arguments
    }
    |> Command.execute

let runContainer () =
    runCLI
        $"""run --name {ContainerName} -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=<YourStrong!Passw0rd>" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2017-latest"""
    |> ignore

let startContainer () =
    runCLI $"start {ContainerName}" |> ignore

let containerExists () =
    let result =
        runCLI $"""ps --all --filter name={ContainerName} --format "{{.ID}}" """

    match result.Text with
    | None -> false
    | Some text -> text <> ""

let containerIsStopped () =
    let result =
        runCLI $"""ps --all --filter name={ContainerName} --format "{{.Status}}" """

    match result.Text with
    | None -> failwith "Error when getting container info"
    | Some text -> text.Contains "Exited"

let waitForContainer () =
    runCLI $"wait --condition running {ContainerName}"
    |> ignore
    // Even though container is running it is not ready for connections
    // Need to wait a bit longer
    // Did not find a clean way of doing this
    async { do! Async.Sleep 5000 } |> Async.RunSynchronously
