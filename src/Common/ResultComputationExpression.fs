module ResultComputationExpression

open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http

open UserMessage

// This is the same as `constant`
// This is used with `let!` when an expression
// can fail (results in the Result type), but does not
// need the ctx
let ignoreContext r _ctx = r

(*

This (reader)-result computation expression has two primary goals

 - 1. To handle failures in an elegant way using the Result type
 - 2. To give access to the context provided by Giraffe, which gives access to the external world
      such as HTTP headers, the DB context, and the apps's config.
 - 3. To have automatic async functions (through the use of Task, which Giraffe also uses)

The following is the available syntax this computation expression implements.

 - `let! x = ...` binds async operations which can fail and needs the context. `x` is then the value of `...` iff `...` evaluates to `Ok x`.
    If it should fail, the whole expression evaluates to this `Error`. The context is passed in as the last parameter.

 - `yield ...` is for side effects, which need access to the context, but cannot fail.

 - `yield! ...` is for async side effects which need access to the context and can fail.
    If the side effect does fail, the whole computation expression evaluates to this `Error`.

 - `return ...` takes a regular value and wraps it up.

 - `return! ...` takes an async "result value" and wraps it up.

*)

type Handler<'t> = HttpContext -> Task<Result<'t, UserMessage list>>

type TaskResultBuilder() =
    member this.Return(x: 'a): Handler<'a> =
        fun _ -> Ok x |> Task.wrap
    member this.ReturnFrom(x) = fun _ -> x
    member this.Yield(f: HttpContext -> unit): Handler<unit> = 
        fun ctx ->
            f ctx
            Ok () |> Task.wrap
    member this.YieldFrom(f: Handler<unit>): Handler<unit> = f
    member this.Delay(f) = f
    member this.Run(f) = f()
    member this.Zero() = this.Return()

    member this.For(sequence, body) =
        fun ctx ->
            sequence
            |> Seq.iter (fun x -> body x ctx |> ignore)
            Ok () |> Task.wrap

    member this.Combine(lhs: Handler<'a>, rhs: unit -> Handler<'b>): Handler<'b> =
        fun ctx ->
            task {
                let! res = lhs ctx 
                return!
                    match res with
                    | Ok _ -> this.Run rhs ctx
                    | Error e -> Error e |> Task.wrap
            }

    member this.Bind(rx: Handler<'a>, f: 'a -> Handler<'b>): Handler<'b> =
        fun ctx -> task {
            let! result = rx ctx
            return!
                match result with
                | Ok x -> f x ctx
                | Error e -> Error e |> Task.wrap
        }

let result = TaskResultBuilder()
