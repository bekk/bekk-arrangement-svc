module App

open System
open Giraffe
open Bekk.Canonical.Logger
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.Data.SqlClient
open Microsoft.Extensions.Logging
open Microsoft.IdentityModel.Tokens
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Authentication.JwtBearer
open Serilog
open Serilog.Events
open Serilog.Formatting.Json

open migrator
open Config
open Email.SendgridApiModels
open TypeHandlers

let webApp (next: HttpFunc) (context: HttpContext) =
    choose [ Health.healthCheck; AuthHandler.config; Handlers.routes ] next context

let configureCors (builder: CorsPolicyBuilder) =
    builder.AllowAnyMethod()
           .AllowAnyHeader()
           .AllowAnyOrigin() |> ignore

let configureApp (configuration : IConfiguration) (app: IApplicationBuilder) =
    app.Use(fun context (next: Func<Task>) ->
        context.Request.Path <- context.Request.Path.Value.Replace (configuration["VIRTUAL_PATH"], "") |> PathString
        next.Invoke())
    |> ignore
    app.UseDefaultFiles() |> ignore
    app.UseStaticFiles() |> ignore
    app.UseAuthentication() |> ignore
    app.UseRouting() |> ignore
    app.UseCors(configureCors) |> ignore
    app.UseOutputCaching()
    app.UseGiraffeErrorHandler(fun (ex : Exception) (logger : Microsoft.Extensions.Logging.ILogger) ->
        logger.LogError(ex, "Unhandled exception")
        clearResponse >=> ServerErrors.INTERNAL_ERROR ex.Message
        ) |> ignore
    app.UseMiddleware<Middleware.RequestLogging>() |> ignore
    app.UseGiraffe(webApp)
    app.UseEndpoints(fun e ->
            // NOTE: The default pattern is {*path:nonfile}, which excludes routes which looks like filenames
            // This means the default will serve "a.b" if you ask for that, or "index.html" if it doesn't look like
            // a file, e.g. "a/b".
            //
            // We use email addresses in the url, and those often looks like "first.last", which is interpreted
            // as files, thus trying to serve a file with that name rather than serve index.html.
            e.MapFallbackToFile("{*path}", "index.html") |> ignore) |> ignore


let configureServices (configuration : IConfiguration) (services: IServiceCollection) =
    services.AddResponseCompression() |> ignore
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    services.AddRouting() |> ignore
    services.AddOutputCaching(fun opt ->
        // The default option requires the user to not be authenticated, probably to avoid leaking data between
        // users. A good default, but it means we cannot use it as a cache for all users. We turn off this security
        // measure so we can use a common cache for all users
        opt.DoesRequestQualify <- fun ctx -> ctx.Request.Method = HttpMethods.Get)
    // Adds secrets needed for communicating with office via microsoft graph
    services.AddSingleton<OfficeEvents.CalendarLookup.Options>(
        { TenantId = configuration["OfficeEvents:TenantId"]
          Mailbox = configuration["OfficeEvents:Mailbox"]
          ClientId = configuration["OfficeEvents:ClientId"]
          ClientSecret = configuration["OfficeEvents:ClientSecret"]
        } : OfficeEvents.CalendarLookup.Options)
    |> ignore
    // Adds all configuration options
    services.AddSingleton<AuthHandler.Config>(
        { EmployeeSvcUrl = configuration["Config:Employee_Svc_url"]
          Audience = configuration["Auth0:Audience"]
          IssuerDomain = configuration["Auth0:Issuer_Domain"]
          Scopes = configuration["Auth0:Scopes"]
        } : AuthHandler.Config) |> ignore
    // Adds sendgrid secrets
    services.AddSingleton<SendgridOptions>
        { ApiKey = configuration["Sendgrid:Apikey"]
          SendgridUrl = configuration["Sendgrid:SendgridUrl"] }
    |> ignore
    let config =
        { isProd = configuration["Auth0:Scheduled_Tasks_Audience"] = "https://api.bekk.no"
          permissionsAndClaimsKey = configuration["Auth0:Permission_Claim_Type"]
          userIdClaimsKey = configuration["Auth0:UserId_Claim"]
          adminPermissionClaim = configuration["Auth0:Admin_Claim"]
          readPermissionClaim = configuration["Auth0:Read_Claim"]
          noReplyEmail = configuration["App:NoReplyEmail"]
          sendMailInDevEnvWhiteList =
              configuration["Sendgrid:Dev_White_List_Addresses"].Split(',')
              |> Seq.toList
              |> List.map (fun s -> s.Trim())
        }
    services.AddScoped<AppConfig>(fun _ -> config) |> ignore
    services.AddScoped<Logger>() |> ignore
    services.AddTransient<SqlConnection>(fun _ -> new SqlConnection(configuration["ConnectionStrings:EventDb"])) |> ignore
    services.AddAuthentication(fun options ->
            options.DefaultAuthenticateScheme <-
                JwtBearerDefaults.AuthenticationScheme
            options.DefaultChallengeScheme <-
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(fun options ->
            let audiences =
                [ configuration["Auth0:Audience"]
                  configuration["Auth0:Ansattlista_iOS_Audience"]
                  configuration["Auth0:Scheduled_Tasks_Audience"] ]
            options.Authority <-
                sprintf "https://%s" configuration["Auth0:Issuer_Domain"]
            options.TokenValidationParameters <-
                TokenValidationParameters
                    (ValidateIssuer = false, ValidAudiences = audiences))
    |> ignore
    DapperConfig.RegisterTypeHandlers()

// To test WebApplication using WebApplicationFactory, we need a Program type.
// This is a hack to get it to compile. The Program will be created for us with the EntryPoint function by the compiler.
type Program() = class end

[<EntryPoint>]
let main args =
    Log.Logger <-
        LoggerConfiguration()
            // Useful to see ports in use for development
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(JsonFormatter(renderMessage=true))
            .CreateLogger()

    try
        try
            Log.Information("Starting web host")
            let builder = WebApplication.CreateBuilder(args)
            builder.Host.UseSerilog() |> ignore
            builder.WebHost
                .UseIISIntegration()
                .UseKestrel(fun _ options -> options.AllowSynchronousIO <- true)
                |> ignore
            configureServices builder.Configuration builder.Services

            let app = builder.Build()
            configureApp app.Configuration app

            if runMigration
            then Migrate.Run(app.Configuration["ConnectionStrings:EventDb"])
            else printfn "Not running migrations. This assumes the database is created and up to date"

            app.Run()
            0
        with ex ->
            Log.Fatal(ex, "Host terminated unexpectedly")
            1
    finally
        Log.CloseAndFlush()
