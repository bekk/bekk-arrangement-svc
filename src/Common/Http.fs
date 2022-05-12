module Http

open System
open Giraffe
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers
    
open UserMessage
open ResultComputationExpression

let check (condition: Handler<Unit>) (next: HttpFunc) (context: HttpContext) =
    task {
        let! checkResult = condition context
        return!
            match checkResult with
            | Ok () -> 
                next context
            | Error errorMessage ->
                let checkFailure = "check_failure", errorMessage |> Seq.map (fun x -> x.ToString()) |> String.concat ";"

                Database.rollbackTransaction context
                convertUserMessagesToHttpError errorMessage next context
    }


let setCsvHeaders (filename:Guid) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        ctx.SetHttpHeader (HeaderNames.ContentType, "text/csv")
        ctx.SetHttpHeader (HeaderNames.ContentDisposition, $"attachment; filename=\"{filename}.csv\"")
        next ctx

let generalHandle (responseBodyFunc: ('t -> HttpHandler)) (endpoint: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
    task {
        let method = context.Request.Method.ToString()
        let path = context.Request.Path.ToString()

        let! res = endpoint context
        return! 
            match res with
            | Ok result ->
                Database.commitTransaction context
                responseBodyFunc result next context
            | Error errorMessage ->
                Database.rollbackTransaction context
                convertUserMessagesToHttpError errorMessage next context
    }


let csvHandle filename (endpoint: Handler<string>) = setCsvHeaders filename >=> generalHandle setBodyFromString endpoint 
let handle (endpoint: Handler<'t>) = generalHandle json endpoint

let getBody<'WriteModel> (): Handler<'WriteModel> =
    fun ctx ->
        try
            Ok(ctx.BindJsonAsync<'WriteModel>().Result) |> Task.wrap
        with _ ->
            Error [ "Feilformatert writemodel" |> BadInput ] |> Task.wrap

let queryParam param =
    result {
        let! res =
            fun ctx ->
                ctx.GetQueryStringValue param
                |> Result.mapError
                    (fun _ ->
                        [ BadInput $"Missing query parameter '{param}'" ])
                |> Task.wrap
        return res
    }

let withTransaction (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
    task {
        try
            Database.createConnection ctx 
            return! handler next ctx
        with _ ->
            Database.rollbackTransaction ctx
            return! convertUserMessagesToHttpError [] next ctx // Default is 500 Internal Server Error
    }

let withRetry (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
    task {
        let seed = Guid.NewGuid().GetHashCode()
        let rnd = Random(seed)

        let rec retry delay amount =
            task {
                try
                    Database.createConnection ctx 
                    return! handler next ctx
                with _ ->
                    Database.rollbackTransaction ctx

                    let jitter = rnd.NextDouble() * 5.0 + 1.5 // [1.5, 6.5]
                    let delayWithJitter =
                        2.0 * delay * jitter + 20.0 * jitter

                    do! Task.Delay (int delayWithJitter)

                    if amount > 0 then
                        return! retry delayWithJitter (amount-1) 
                    else
                        return! convertUserMessagesToHttpError [] next ctx 
            }

        return! retry 50.0 10 // retry 10 times with a inital delay seed 50ms
    }

let withLock (lock: SemaphoreSlim) (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
    task {
        do! lock.WaitAsync() 

        let! res = handler next ctx

        lock.Release() |> ignore

        return res
    }

let parseBody<'T> =
    result {
        let! body = 
            fun ctx ->
                ctx.ReadBodyBufferedFromRequestAsync()
                |> Task.map Ok

        let res = Thoth.Json.Net.Decode.Auto.fromString<'T> body
        match res with
        | Ok x ->
            return x
        | Error _ ->
            return!
                Error [ BadInput $"Kunne ikke parse body: {body}" ]
                |> Task.wrap
    }
