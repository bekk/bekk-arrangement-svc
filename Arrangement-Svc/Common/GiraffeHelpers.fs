module GiraffeHelpers

open Giraffe
open Microsoft.AspNetCore.Http

// These functions are copies of Giraffes implementation
// The difference here is that we get the logger from the context and log the template path
let route (path : string) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let logger = ctx.GetService<Bekk.Canonical.Logger.Logger>()
        logger.log("template_path", path)
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
                let logger = ctx.GetService<Bekk.Canonical.Logger.Logger>()
                logger.log("template_path", path.Value)
                routeHandler args next ctx
