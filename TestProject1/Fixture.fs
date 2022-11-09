namespace Tests

open System
open Xunit
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration

open Utils

type DatabaseFixture() =
    let mutable updateDb = false
    do
        Environment.SetEnvironmentVariable("NO_MIGRATION", "1")
        if Config.manageContainers then
            if Container.containerMissing() then
                printfn "Container missing. Creating fresh container for tests."
                Container.create()
                Container.start()
                updateDb <- true
            elif Container.containerStopped() then
                printfn "Container already exists. Reusing container for tests."
                Container.start()
            else
                printfn "Container already up and running. Reusing container for tests."
    let _factory = new WebApplicationFactory<App.Program>()
    let config = _factory.Services.GetService(typeof<IConfiguration>) :?> IConfiguration
    do
        if updateDb then
            Database.create(config.GetConnectionString("EventDb"))
            Database.migrate(config.GetConnectionString("EventDb"))
    member this.factory = _factory
    member this.token = Environment.GetEnvironmentVariable "ARRANGEMENT_SVC_TEST_JWT_TOKEN"
    interface IDisposable with
        member this.Dispose () = _factory.Dispose()

[<CollectionDefinition("Database collection")>]
type DatabaseCollection() =
    interface ICollectionFixture<DatabaseFixture>
