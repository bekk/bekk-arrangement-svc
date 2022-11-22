module rec Container

open System
open Fli

open Utils

// let ContainerName =
//     Environment.GetEnvironmentVariable("ARRANGEMENT_SVC_TESTCONTAINER")
//     |> Option.ofObj
//     |> Option.defaultValue "Arrangement_Svc_TestContainer"
//
let ContainerManagerProgram =
    Environment.GetEnvironmentVariable("ARRANGEMENT_SVC_CONTAINER_MANAGER")
    |> Option.ofObj
    |> Option.defaultValue "podman"
//
// let private container (args : string seq) =
//     printfn "In container with args: %A" args
//     let p =
//         let i = System.Diagnostics.ProcessStartInfo()
//         i.FileName <-ContainerManagerProgram
//         i.Arguments <- $"{String.Join(' ', args)}"
//         i.UseShellExecute <- false
//         i.RedirectStandardError <- true
//         i.RedirectStandardOutput <- true
//         i.RedirectStandardInput <- true
//         #if DEBUG_CONTAINER
//         printfn $"{ContainerManagerProgram} COMMAND: %A{i.FileName} %A{i.Arguments}"
//         #endif
//         System.Diagnostics.Process.Start(i)
//     printfn "after P"
//     // p.WaitForExit()
//     let out = p.StandardOutput.ReadToEnd()
//     printfn "OUT: %A" out
//     let err = p.StandardError.ReadToEnd()
//     printfn "err: %A" err
//     if not (String.IsNullOrWhiteSpace(err)) then failwithf "%A failed! Error: %A\nOutput: %A" ContainerManagerProgram err out
//     #if DEBUG_CONTAINER
//     printfn $"{ContainerManagerProgram} OUTPUT: %A{out}"
//     #endif
//     out
//
// let getContainer () : string option =
//     container [$"ps --all --filter name={ContainerName} --format {{{{.ID}}}}"]
//     |> Option.fromString
//
// let containerExists : unit -> bool =
//     getContainer >> Option.isSome
//
// let containerMissing : unit -> bool =
//     not << containerExists
//
// let private getStatus () : string option =
//     container [$"ps --all --filter name={ContainerName} --format {{{{.Status}}}}"]
//     |> Option.fromString
//
// let containerRunning : unit -> bool =
//     let status = getStatus ()
//     printfn "STATUS: %A" status
//     getStatus
//     >> Option.filter (matches "^Up ")
//     >> Option.isSome
//
// let containerStopped : unit -> bool =
//     not << containerRunning
//
// let private waitForContainerRunning () : unit =
//     container [$"wait --condition running {ContainerName}"] |> ignore
//
// let create () =
//     startContainer()
//     printfn $"Run container {ContainerName}"
//     container [$"run --name {ContainerName} -e \"ACCEPT_EULA=Y\" -e \"SA_PASSWORD=<YourStrong!Passw0rd>\" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2017-latest"] |> ignore
//
// let rm () =
//     if Option.isNone <| getContainer()
//     then printfn "No container exists, ignoring rm command."
//     else
//     printfn $"Deleting container {ContainerName}"
//     container [$"rm {ContainerName}"] |> ignore
//
// let kill () =
//     if containerStopped()
//     then printfn "Container not running, ignoring kill command."
//     else
//     printfn $"Stopping container {ContainerName}"
//     container [$"kill {ContainerName}"] |> ignore
//
// let start () =
//     if containerRunning()
//     then printfn "Container already running, ignoring start command."
//     else
//     printfn $"Starting container {ContainerName}"
//     container [$"start {ContainerName}"] |> ignore
//     async { do! Async.Sleep 10000 } |> Async.RunSynchronously
//     waitForContainerRunning ()

let runContainer () =
    cli{
        Exec ContainerManagerProgram
        Arguments $"""run --name Arrangement_Svc_TestContainer -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=<YourStrong!Passw0rd>" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2017-latest"""
    }
    |> Command.execute
    |> ignore
    ()

let startContainer () =
    cli{
        Exec ContainerManagerProgram
        Arguments "start Arrangement_Svc_TestContainer"
    }
    |> Command.execute
    |> ignore
    ()

let containerExists () =
    printfn "Container exists?"
    let result =
        cli {
            Exec ContainerManagerProgram
            Arguments """ps --all --filter name=Arrangement_Svc_TestContainer --format "{{.ID}}" """
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
            Arguments """ps --all --filter name=Arrangement_Svc_TestContainer --format "{{.Status}}"""
        }
        |> Command.execute
    match result.Text with
    | None -> failwith "Error when getting container info"
    | Some text ->
        text.Contains "Exited"

let waitForContainer() =
     cli {
         Exec ContainerManagerProgram
         Arguments "wait --condition running Arrangement_Svc_TestContainer"
     }
     |> Command.execute
     |> ignore

let containerIsRunning() =
    let result =
        cli {
            Exec ContainerManagerProgram
            Arguments """ps --all --filter name=Arrangement_Svc_TestContainer --format "{{.Status}}"""
        }
        |> Command.execute
    match result.Text with
    | None -> failwith "Error when getting container info"
    | Some text ->
        text.Contains "Up"

let makeSureIsRunning () =
    if containerIsStopped () then
        startContainer ()
    if containerIsRunning () then
        waitForContainer()

// let rec makeSureContainerIsRunning () =
//     let result =
//         let result =
//             cli {
//                 Exec "podman"
//                 Arguments """ps --all --filter name=Arrangement_Svc_TestContainer --format "{{.Status}}"""
//             }
//             |> Command.execute
//         match result.Text with
//         | Some text -> text
//         | None -> failwith "Error when getting container info"
//
//     printfn "REUSLT IS: %A" result
//
//     if result.Contains "Exited" then
//         cli {
//             Exec "podman"
//             Arguments "start Arrangement_Svc_TestContainer"
//         }
//         |> Command.execute
//         |> ignore
//
//     if result.Contains "Up" then
//         // cli {
//         //     Exec "podman"
//         //     Arguments "start Arrangement_Svc_TestContainer"
//         // }
//         // |> Command.execute
//         // |> ignore
//         cli {
//             Exec "podman"
//             Arguments "wait --condition running Arrangment_Svc_TestContainer"
//         }
//         |> Command.execute
//         |> ignore
