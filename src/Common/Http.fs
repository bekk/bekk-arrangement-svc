namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks

open ArrangementService
open UserMessage
open System

module Http =

    type Handler<'t> = HttpContext -> Result<'t, UserMessage list>

    let check (condition: Handler<Unit>) (next: HttpFunc) (context: HttpContext) =
        Database.createConnection context |> ignore
        match condition context with
        | Ok() -> next context
        | Error errorMessage ->
            convertUserMessagesToHttpError errorMessage next context

    let handle (endpoint: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
        let conn, transaction = Database.createConnection context
        match endpoint context with
        | Ok result ->
            transaction.Commit()
            conn.Close()
            json result next context
        | Error errorMessage ->
            transaction.Rollback()
            conn.Close()
            convertUserMessagesToHttpError errorMessage next context

    let getBody<'WriteModel> (context: HttpContext): Result<'WriteModel, UserMessage list>
        =
        try
            Ok(context.BindJsonAsync<'WriteModel>().Result)
        with _ -> Error [ "Feilformatert writemodel" |> BadInput ]

    let queryParam param (ctx: HttpContext) =
        ctx.GetQueryStringValue param
        |> Result.mapError
            (fun _ ->
                [ BadInput $"Missing query parameter '{param}'" ])

    let withRetry (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        let seed = Guid.NewGuid().GetHashCode()
        let rnd = Random(seed)
        let rec retry delay amount =
            try
                handler next ctx
            with _ ->
                let config = Config.getConfig ctx
                config.currentTransaction.Rollback()
                config.currentConnection.Close()
                config.currentConnection <- null
                config.currentTransaction <- null

                let jitter = rnd.NextDouble() + 0.5 // [0.5, 1.5]
                let delayWithJitter =
                    2.0 * delay * jitter + 20.0 * jitter

                Async.Sleep (int delayWithJitter)
                |> Async.RunSynchronously

                if amount > 0 then
                    retry delayWithJitter (amount-1) 
                else
                    ServerErrors.internalError (text "Beklager, noe gikk galt") next ctx
                    

        retry 50.0 5 // retry 5 times with a inital delay seed 50ms

    let parseBody<'T> (ctx: HttpContext) =
        let body = 
            ctx.ReadBodyBufferedFromRequestAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Thoth.Json.Net.Decode.Auto.fromString<'T> body
        |> function
        | Ok x -> Ok x
        | Error _ -> Error [ BadInput $"Kunne ikke parse body: {body}" ]
