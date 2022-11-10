namespace Tests

open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Xunit
open System

open AuthHandler

type DatabaseFixture() =
    inherit WebApplicationFactory<App.Program>()
    let mutable updateDb = false

    do
        Environment.SetEnvironmentVariable("NO_MIGRATION", "1")

        if Config.manageContainers then
            if Container.containerMissing () then
                printfn "Container missing. Creating fresh container for tests."
                Container.create ()
                Container.start ()
                updateDb <- true
            elif Container.containerStopped () then
                printfn "Container already exists. Reusing container for tests."
                Container.start ()
            else
                printfn "Container already up and running. Reusing container for tests."

    do
        if updateDb then
            Database.create ("Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db")
            Database.migrate ("Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db")

    override this.ConfigureWebHost(builder: IWebHostBuilder) =
        builder.ConfigureTestServices (fun (services: IServiceCollection) ->
            services
                .AddAuthentication(fun options ->
                    options.DefaultAuthenticateScheme <- "Test"
                    options.DefaultScheme <- "Test"
                    ())
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", (fun options -> ()))
            |> ignore)
        |> ignore

[<CollectionDefinition("Database collection")>]
type DatabaseCollection() =
    interface ICollectionFixture<DatabaseFixture>
