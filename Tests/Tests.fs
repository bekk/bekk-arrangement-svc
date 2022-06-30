module Tests.All

open System
open Expecto
open Microsoft.Data.SqlClient

open TestUtils
open migrator

let allTests =
    testList "All Tests" [
        General.tests
        CreateEvent.tests
        RegisterToEvent.tests
        UpdateEvent.tests
        GetEvent.tests
        DeleteEvent.tests
        OfficeEvents.tests
    ]

module Podman =

    [<Literal>]
    let ContainerName = "Arrangement_Svc_TestContainer"

    let private podman (args : string seq) =
        let p =
            let i = System.Diagnostics.ProcessStartInfo()
            i.FileName <- "sudo"
            i.Arguments <- $"podman {String.Join(' ', args)}"
            i.UseShellExecute <- false
            i.RedirectStandardError <- true
            i.RedirectStandardOutput <- true
            i.RedirectStandardInput <- true
            #if DEBUG_PODMAN
            printfn $"PODMAN COMMAND: %A{i.FileName} %A{i.Arguments}"
            #endif
            System.Diagnostics.Process.Start(i)
        p.WaitForExit()
        let out = p.StandardOutput.ReadToEnd()
        let err = p.StandardError.ReadToEnd()
        if not (String.IsNullOrWhiteSpace(err)) then failwithf "Podman failed! Error: %A\nOutput: %A" err out
        #if DEBUG_PODMAN
        printfn $"PODMAN OUTPUT: %A{out}"
        #endif
        out

    let getContainer () : string option =
        podman [$"ps --all --filter name={ContainerName} --format {{{{.ID}}}}"]
        |> Option.fromString

    let containerExists : unit -> bool =
        getContainer >> Option.isSome

    let containerMissing : unit -> bool =
        not << containerExists

    let private getStatus () : string option =
        podman [$"ps --all --filter name={ContainerName} --format {{{{.Status}}}}"]
        |> Option.fromString

    let containerRunning : unit -> bool =
        getStatus
        >> Option.filter (matches "^Up ")
        >> Option.isSome

    let containerStopped : unit -> bool =
        not << containerRunning

    let private waitForContainerRunning () : unit =
        while containerStopped() do
            printfn "Waiting for container to start .."
            Threading.Thread.Sleep(1000)

    let create () =
        printfn $"Run container {ContainerName}"
        // TODO: Fetch password from config
        podman [$"create --name {ContainerName} -e \"ACCEPT_EULA=Y\" -e \"SA_PASSWORD=<YourStrong!Passw0rd>\" -p 1433:1433 mcr.microsoft.com/mssql/server:2017-latest"] |> ignore

    let rm () =
        if Option.isNone <| getContainer()
        then printfn "No container exists, ignoring rm command."
        else
        printfn $"Deleting container {ContainerName}"
        podman [$"rm {ContainerName}"] |> ignore

    let kill () =
        if containerStopped()
        then printfn "Container not running, ignoring kill command."
        else
        printfn $"Stopping container {ContainerName}"
        podman [$"kill {ContainerName}"] |> ignore

    let start () =
        if containerRunning()
        then printfn "Container already running, ignoring start command."
        else
        printfn $"Starting container {ContainerName}"
        podman [$"start {ContainerName}"] |> ignore
        waitForContainerRunning ()

module Database =
    let private getConnectionString () : string =
        let connectionString = App.configuration["ConnectionStrings:EventDb"]
        let cs = SqlConnectionStringBuilder(connectionString)
        cs.InitialCatalog <- ""
        cs.ConnectionString

    let create () : unit =
        if Podman.containerStopped() then failwith "Cannot create database, container not running."
        printfn "Creating database"
        use connection = new SqlConnection(getConnectionString())
        connection.Open()
        use command = connection.CreateCommand()
        command.CommandText <- "CREATE DATABASE [arrangement-db];"
        command.ExecuteNonQuery() |> ignore
        connection.Close()

    let migrate () : unit =
        printfn "Migrating database"
        Migrate.Run(getConnectionString())

let enforceTokenExists() =
    if String.IsNullOrWhiteSpace(token) then
        failwith $"Missing JWT token in environment variable {TokenEnvVariableName}. Use token from other dev system.
            \nStart with `{TokenEnvVariableName}=MYTOKEN dotnet run`, set it in Rider, or otherwise make it available before running tests."

let maybeUpdateDatabase() =
    if Config.runMigration
    then
        Database.create()
        Database.migrate()
    else printfn "Not running migrations. This assumes the database is created and up to date"

[<EntryPoint>]
let main args =
    enforceTokenExists()

    if Config.runPodman then
        if Podman.containerMissing() then
            printfn "Container missing. Creating fresh container for tests."
            Podman.create()
            Podman.start()
            maybeUpdateDatabase()
        elif Podman.containerStopped() then
            printfn "Container already exists. Reusing container for tests."
            Podman.start()
        else
            printfn "Container already up and running. Reusing container for tests."
    else
        printfn "Running tests without podman interaction. This assumes the test container is running properly."
        maybeUpdateDatabase()

    runTestsWithCLIArgs [] args allTests
