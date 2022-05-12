module Logging

open System
open Giraffe
open Serilog
open Serilog.Events
open Serilog.Formatting.Json
open Microsoft.AspNetCore.Http
open Giraffe.SerilogExtensions
open Microsoft.Extensions.Logging

type ExceptionType =
    { LogEventId: string
      LogLevel: LogLevel
      ExceptionType: Type
      ExceptionMessage: string
      Name: string
      UserId: string
      UserMessage: string
      RequestUrl: string
      RequestMethod: string
      RequestConsumerName: string
      RequestTraceId: string
      StackTrace: string
      StatusCode: int
      InnerException: exn
    }

let getEmployeeName (ctx: HttpContext) =
    let nameClaim = ctx.User.FindFirst "name"
    match nameClaim with
    | null -> "Fant ikke brukernavn"
    | _ -> nameClaim.Value

let getEmployeeId (ctx: HttpContext) =
    let idClaim =
        ctx.User.FindFirst(ctx.GetService<Config.AppConfig>().userIdClaimsKey)
    match idClaim with
    | null -> "Fant ikke bruker-id"
    | _ -> idClaim.Value

let getConsumerName (ctx: HttpContext) =
    let hasValue, value = ctx.Request.Headers.TryGetValue("X-ConsumerName")
    if hasValue then
        value.ToString()
    else
        "Consumer name is not set. Make sure to set the X-ConsumerName header"

let createExceptionMessage (ex: Exception) (ctx: HttpContext) =
    let logEventId =
        (Config.getConfig ctx).requestId
    let userMessage =
        sprintf
            "Beklager det skjedde en feil! Den er logget med id %s, ta kontakt med Basen om du ønsker videre oppfølging."
    { LogEventId = logEventId
      LogLevel = LogLevel.Error
      ExceptionType = ex.GetType()
      ExceptionMessage = ex.Message
      Name = getEmployeeName ctx
      UserId = getEmployeeId ctx
      UserMessage = userMessage logEventId
      RequestUrl = ctx.GetRequestUrl()
      RequestMethod = ctx.Request.Method
      RequestConsumerName = getConsumerName ctx
      RequestTraceId = ctx.TraceIdentifier
      StackTrace = ex.StackTrace
      StatusCode = StatusCodes.Status500InternalServerError
      InnerException = ex.InnerException }

let errorHandler (ex: Exception): HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let exceptionMessage = createExceptionMessage ex ctx
        let logger = ctx.Logger()
        logger.Error("{@Logmessage}", exceptionMessage)
        json exceptionMessage next ctx

let config =
    { SerilogConfig.defaults with
          ErrorHandler = fun ex _ -> setStatusCode 500 >=> errorHandler ex }
let createLoggingApp webApp config = SerilogAdapter.Enable(webApp, config)

Log.Logger <-
    LoggerConfiguration().Enrich.FromLogContext()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Warning()
        .WriteTo.Console(JsonFormatter())
        .CreateLogger()