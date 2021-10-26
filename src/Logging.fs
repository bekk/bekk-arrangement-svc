namespace ArrangementService

open System
open Giraffe
open Serilog
open Microsoft.AspNetCore.Http
open Giraffe.SerilogExtensions
open Serilog.Formatting.Json
open Microsoft.Extensions.Logging
open Serilog.Events

module Logging =
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
            ctx.User.FindFirst(ctx.GetService<AppConfig>().userIdClaimsKey)
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
            Guid.NewGuid().ToString().Split(Convert.ToChar("-")).[0]
        let userMessage =
            sprintf
                "Beklager det skjedde en feil! Den er logget med id %s Ta kontakt med Basen om du ønsker videre oppfølging."
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

    // 👆 Garbage ferdig her:
    
    let log (logLineDescription: string) (data: (string * string) seq) (ctx: HttpContext) =
        let utcTimeStamp =
            DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss")
            
        let keyValuePairs: string =
            data
            |> Seq.map (fun (k,v) -> $"{k}={v}")
            |> String.concat " "
            
        let logMessage =
            sprintf "[%sZ] %s %s" utcTimeStamp logLineDescription keyValuePairs
        
        let logger = ctx.Logger()
        logger.Information("{@Logmessage}", logMessage)
        Ok () |> Task.wrap
