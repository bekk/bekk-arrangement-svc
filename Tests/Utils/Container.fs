module Container

open System

open Utils

let ContainerName =
    Environment.GetEnvironmentVariable("ARRANGEMENT_SVC_TESTCONTAINER")
    |> Option.ofObj
    |> Option.defaultValue "Arrangement_Svc_TestContainer"

let ContainerManagerProgram =
    Environment.GetEnvironmentVariable("ARRANGEMENT_SVC_CONTAINER_MANAGER")
    |> Option.ofObj
    |> Option.defaultValue "podman"

let private container (args : string seq) =
    let p =
        let i = System.Diagnostics.ProcessStartInfo()
        i.FileName <-ContainerManagerProgram
        i.Arguments <- $"{String.Join(' ', args)}"
        i.UseShellExecute <- false
        i.RedirectStandardError <- true
        i.RedirectStandardOutput <- true
        i.RedirectStandardInput <- true
        #if DEBUG_CONTAINER
        printfn $"{ContainerManagerProgram} COMMAND: %A{i.FileName} %A{i.Arguments}"
        #endif
        System.Diagnostics.Process.Start(i)
    p.WaitForExit()
    let out = p.StandardOutput.ReadToEnd()
    let err = p.StandardError.ReadToEnd()
    if not (String.IsNullOrWhiteSpace(err)) then failwithf "%A failed! Error: %A\nOutput: %A" ContainerManagerProgram err out
    #if DEBUG_CONTAINER
    printfn $"{ContainerManagerProgram} OUTPUT: %A{out}"
    #endif
    out

let getContainer () : string option =
    container [$"ps --all --filter name={ContainerName} --format {{{{.ID}}}}"]
    |> Option.fromString

let containerExists : unit -> bool =
    getContainer >> Option.isSome

let containerMissing : unit -> bool =
    not << containerExists

let private getStatus () : string option =
    container [$"ps --all --filter name={ContainerName} --format {{{{.Status}}}}"]
    |> Option.fromString

let containerRunning : unit -> bool =
    getStatus
    >> Option.filter (matches "^Up ")
    >> Option.isSome

let containerStopped : unit -> bool =
    not << containerRunning

let private waitForContainerRunning () : unit =
    container [$"wait --condition running {ContainerName}"] |> ignore

let create () =
    printfn $"Run container {ContainerName}"
    // TODO: Fetch password from config
    container [$"create --name {ContainerName} -e \"ACCEPT_EULA=Y\" -e \"SA_PASSWORD=<YourStrong!Passw0rd>\" -p 1433:1433 mcr.microsoft.com/mssql/server:2017-latest"] |> ignore

let rm () =
    if Option.isNone <| getContainer()
    then printfn "No container exists, ignoring rm command."
    else
    printfn $"Deleting container {ContainerName}"
    container [$"rm {ContainerName}"] |> ignore

let kill () =
    if containerStopped()
    then printfn "Container not running, ignoring kill command."
    else
    printfn $"Stopping container {ContainerName}"
    container [$"kill {ContainerName}"] |> ignore

let start () =
    if containerRunning()
    then printfn "Container already running, ignoring start command."
    else
    printfn $"Starting container {ContainerName}"
    container [$"start {ContainerName}"] |> ignore
    async { do! Async.Sleep 10000 } |> Async.RunSynchronously
    waitForContainerRunning ()
