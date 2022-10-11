module GiraffeHelpers

open Giraffe
open Microsoft.AspNetCore.Http

// Datadog gets resource name "GET /{** path}", and we need to set it manually.
let private setDatadogResourceName (ctx: HttpContext) (path: string) : unit =
    let scope = Datadog.Trace.Tracer.Instance.ActiveScope
    if isNotNull scope then scope.Span.ResourceName <- $"{ctx.Request.Method} {path}"
    ()

// These functions are copies of Giraffes implementation
// The difference here is that we get the logger from the context and log the template path
let route (path : string) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        setDatadogResourceName ctx path
        if (SubRouting.getNextPartOfPath ctx).Equals path
        then next ctx
        else skipPipeline

let routef (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
    FormatExpressions.validateFormat path
    fun (next : HttpFunc) (ctx : HttpContext) ->
        FormatExpressions.tryMatchInput path FormatExpressions.MatchOptions.Exact (SubRouting.getNextPartOfPath ctx)
        |> function
            | None      -> skipPipeline
            | Some args ->
                setDatadogResourceName ctx path.Value
                routeHandler args next ctx
