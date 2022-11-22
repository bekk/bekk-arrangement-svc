namespace Tests

open System.Security.Claims
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Xunit
open System

open AuthHandler

type DatabaseFixture() =
    inherit WebApplicationFactory<App.Program>()

    // If manage containers
        // If container is missing
            // Start
            // Make sure is running
            // Migrate Database
        // If container is stopepd
            // Start container
        // Else
            // Container is alreayd runniong
    do
        Environment.SetEnvironmentVariable("NO_MIGRATION", "1")
        printfn "Before mangage containers"
        if Config.manageContainers then
            printfn "Manage containers"
            let doesExist = Container.containerExists()
            printfn "Exists? %A" doesExist
            if not doesExist then
                printfn "Starting contauiner"
                Container.runContainer()
                printfn "Waitinf for containers"
                Container.waitForContainer()
                printfn "10s wait"
                async { do! Async.Sleep 10000 } |> Async.RunSynchronously
                Database.create "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"
                Database.migrate "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"
            if Container.containerIsStopped () then
                printfn "Container is stopped"
                Container.startContainer()
                Container.waitForContainer()
                async { do! Async.Sleep 10000 } |> Async.RunSynchronously
        else
            printfn "Container already exists"
            ()

        // printfn "BEFOER FOO"
        // Container.startContainer()
        // Container.makeSureContainerIsRunning()
        // printfn "AFTER FOO"
        // async { do! Async.Sleep 10000 } |> Async.RunSynchronously

        // if Config.manageContainers then
        //     if Container.containerMissing () then
        //         printfn "Container missing. Creating fresh container for tests."
        //         Container.create ()
        //         Container.start ()
        // Database.create "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"
        // Database.migrate "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"
        //     elif Container.containerStopped () then
        //         printfn "Container already exists. Reusing container for tests."
        //         Container.start (i
        //     else
        //         printfn "Container already up and running. Reusing container for tests."

    member this.getAuthedClient =
        this
            .WithWebHostBuilder(fun builder ->
                builder.ConfigureTestServices (fun (services: IServiceCollection) ->
                    services
                        .AddAuthentication(fun options ->
                            options.DefaultAuthenticateScheme <- "Test"
                            options.DefaultScheme <- "Test"
                            ())
                        .AddScheme<TestAuthHandlerOptions, TestAuthHandler>("Test", (fun options -> ()))
                    |> ignore)
                |> ignore)
            .CreateClient()

    member this.getAuthedClientWithClaims (employeeId: int) (permissions: string list) =
        this
            .WithWebHostBuilder(fun builder ->
                builder.ConfigureTestServices (fun (services: IServiceCollection) ->
                    services.Configure<TestAuthHandlerOptions> (fun (options: TestAuthHandlerOptions) ->
                        options.EmployeeId <- employeeId
                        options.BekkPermissions <- permissions)
                    |> ignore

                    services
                        .AddAuthentication(fun options ->
                            options.DefaultAuthenticateScheme <- "Test"
                            options.DefaultScheme <- "Test"
                            ())
                        .AddScheme<TestAuthHandlerOptions, TestAuthHandler>("Test", (fun options -> ()))
                    |> ignore)
                |> ignore)
            .CreateClient()

    member this.getUnauthenticatedClient =
        this
            .WithWebHostBuilder(fun builder ->
                builder.ConfigureTestServices(fun services -> ())
                |> ignore)
            .CreateClient()

[<CollectionDefinition("Database collection")>]
type DatabaseCollection() =
    interface ICollectionFixture<DatabaseFixture>
