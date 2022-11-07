module Tests

open System
open Xunit
open Xunit.Abstractions
open Microsoft.AspNetCore.Mvc.Testing

type Program = class end

type Foo(factory: WebApplicationFactory<Program>) =
    interface IClassFixture<WebApplicationFactory<Program>>

type BasicFixture() =
    interface IDisposable with
        member this.Dispose () =
            ()

type Tests() =
    [<Fact>]
    let ``My test`` () =
        Assert.True(true)

    interface IClassFixture<Foo> with
