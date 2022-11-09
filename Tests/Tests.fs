module Tests.All

open System
open Expecto
open Microsoft.Data.SqlClient
open Microsoft.Extensions.Configuration

open TestUtils
open migrator

let allTests =
    testList "All Tests" [
        DateTimeCustom.tests
        General.tests
        CreateEvent.tests
        RegisterToEvent.tests
        UpdateEvent.tests
        SendEmailOnUpdateEvent.tests
        GetEvent.tests
        DeleteEvent.tests
        OfficeEvents.tests
    ]

module Container =
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
            i.FileName <- "sudo"
            i.Arguments <- $"{ContainerManagerProgram} {String.Join(' ', args)}"
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
        async { do! Async.Sleep 5000 } |> Async.RunSynchronously
        waitForContainerRunning ()

module Database =
    let private getConnectionString () : string =
        let cfg = Api.webapp().Services.GetService(typeof<IConfiguration>) :?> IConfiguration
        let connectionString = cfg.GetConnectionString("EventDb")
        let cs = SqlConnectionStringBuilder(connectionString)
        cs.InitialCatalog <- ""
        cs.ConnectionString

    let create () : unit =
        if Container.containerStopped() then failwith "Cannot create database, container not running."
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
    if String.IsNullOrWhiteSpace(Api.token) then
        failwith $"Missing JWT token in environment variable {Api.TokenEnvVariableName}. Use token from other dev system.
            \nStart with `{Api.TokenEnvVariableName}=MYTOKEN dotnet run`, set it in Rider, or otherwise make it available before running tests."

let maybeUpdateDatabase() =
    if Config.runMigration
    then
        Database.create()
        Database.migrate()
    else printfn "Not running migrations. This assumes the database is created and up to date"

[<EntryPoint>]
let main args =
    enforceTokenExists()
    printfn "Enforce token exists"

    if Config.manageContainers then
        printfn "Manage conrtainers"
        if Container.containerMissing() then
            printfn "Container missing. Creating fresh container for tests."
            Container.create()
            Container.start()
            maybeUpdateDatabase()
        elif Container.containerStopped() then
            printfn "Container already exists. Reusing container for tests."
            Container.start()
        else
            printfn "Container already up and running. Reusing container for tests."
    else
        printfn "Running tests without container interaction. This assumes the test container is running properly."
        maybeUpdateDatabase()

    runTestsWithCLIArgs [] args allTests
