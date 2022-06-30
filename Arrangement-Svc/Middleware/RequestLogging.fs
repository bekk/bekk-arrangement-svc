module Middleware

open Giraffe
open System
open System.Diagnostics
open Bekk.Canonical.Logger
open Microsoft.AspNetCore.Http

type RequestLogging(next: RequestDelegate) =
    member this.Invoke(ctx: HttpContext, logger: Logger) =
        task {
            let! loggedInEmployee = Auth.getUserId ctx
            let method = ctx.Request.Method
            let path = ctx.Request.Path.ToString()
            let consumer =
                ctx.Request.Headers["X-From"].ToArray()
                |> String.concat "; "
                
            logger.log([
                "method", method
                "request_path", path
            ])
            
            if loggedInEmployee.IsSome then
               logger.log("logged_in_employee", loggedInEmployee.Value)
               
            if String.IsNullOrEmpty consumer |> not then
                logger.log("request_sent_from", consumer)
                
            let stopwatch = Stopwatch.StartNew()
            do! next.Invoke(ctx)
            
            let code = ctx.Response.StatusCode.ToString()
            let elapsedTime = stopwatch.ElapsedMilliseconds
            logger.log([
                "duration", elapsedTime
                "statusCode", code
            ])
        }


[<AutoOpen>]
module OutputCache =
    open WebEssentials.AspNetCore.OutputCaching
    let outputCache (f : OutputCacheProfile -> unit) =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let profile = OutputCacheProfile()
            f profile
            ctx.EnableOutputCaching(profile)
            next ctx
