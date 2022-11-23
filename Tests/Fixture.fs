namespace Tests

open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Xunit
open System

open AuthHandler

type DatabaseFixture() =
    inherit WebApplicationFactory<App.Program>()
    let connectionString = "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"
    do
        Environment.SetEnvironmentVariable("NO_MIGRATION", "1")
        if Config.manageContainers then
            let doesExist = Container.containerExists()
            if not doesExist then
                printfn "Container missing. Creating fresh container for tests."
                Container.runContainer()
                Container.waitForContainer()
                Database.create connectionString
                Database.migrate connectionString
            if Container.containerIsStopped () then
                printfn "Container already exists. Reusing container for tests."
                Container.startContainer()
                Container.waitForContainer()
        else
            printfn "Container already up and running. Reusing container for tests."

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

    member this.getAuthedClient = this.getAuthedClientWithClaims 0 []

    member this.getUnauthenticatedClient =
        this
            .WithWebHostBuilder(fun builder ->
                builder.ConfigureTestServices(fun services -> ())
                |> ignore)
            .CreateClient()

[<CollectionDefinition("Database collection")>]
type DatabaseCollection() =
    interface ICollectionFixture<DatabaseFixture>
