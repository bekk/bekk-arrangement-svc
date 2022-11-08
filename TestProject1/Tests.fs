namespace Tests

open System
open Xunit
open FsUnit.Xunit
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration

type DatabaseFixture() =
    let factory = new WebApplicationFactory<App.Program>()
    let config = factory.Services.GetService(typeof<IConfiguration>) :?> IConfiguration
    interface IDisposable with
        member this.Dispose () = factory.Dispose()

[<CollectionDefinition("Database collection")>]
type DatabaseCollection() =
    interface ICollectionFixture<DatabaseFixture>

[<Collection("Database collection")>]
type Tests() =
    [<Fact>]
    member _.``My test`` () =
        true |> should be True

[<Collection("Database collection")>]
type Tests2() =
    [<Fact>]
    member _.``My test 2`` () =
        true |> should be True
