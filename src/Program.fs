﻿module ArragementService.App

open System
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open System.IO
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Hosting
open Giraffe.Serialization
open Thoth.Json.Net
open System.Data
open System.Data.SqlClient

open ArrangementService
open migrator
open Logging
open SendgridApiModels

let webApp =
    choose
        [ Health.healthCheck; Event.Handlers.routes; Participant.Handlers.routes ]

let private configuration =
    let builder = ConfigurationBuilder()
    builder.AddJsonFile("appsettings.json") |> ignore
    builder.AddEnvironmentVariables() |> ignore
    builder.Build()

let configureCors (builder: CorsPolicyBuilder) =
    builder.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin() |> ignore

let configureApp (app: IApplicationBuilder) =
    app.Use(fun context next ->
        context.Request.Path <-
            context.Request.Path.Value.Replace
                (configuration.["VIRTUAL_PATH"], "") |> PathString
        next.Invoke())
    |> ignore
    app.UseAuthentication().UseCors(configureCors)
       .UseGiraffe(createLoggingApp webApp config)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    let connection: IDbConnection = 
        new SqlConnection(configuration.["ConnectionStrings:EventDb"]) :> IDbConnection
    services.AddSingleton<IDbConnection>(connection) |> ignore
    services.AddSingleton<SendgridOptions>
        ({ ApiKey = configuration.["Sendgrid:Apikey"]
           SendgridUrl = configuration.["Sendgrid:SendgridUrl"] })
    |> ignore
    let config =
        { isProd = configuration.["Auth0:Scheduled_Tasks_Audience"] = "https://api.bekk.no"
          permissionsAndClaimsKey = configuration.["Auth0:Permission_Claim_Type"]
          userIdClaimsKey = configuration.["Auth0:UserId_Claim"]
          adminPermissionClaim = configuration.["Auth0:Admin_Claim"]
          readPermissionClaim = configuration.["Auth0:Read_Claim"]
          noReplyEmail = configuration.["App:NoReplyEmail"]
          sendMailInDevEnvWhiteList =
              configuration.["Sendgrid:Dev_White_List_Addresses"].Split(',')
              |> Seq.toList
              |> List.map (fun s -> s.Trim()) }
    services.AddSingleton<AppConfig> config |> ignore // For å sende mail: bytt ut = med <>
    services.AddAuthentication(fun options ->
            options.DefaultAuthenticateScheme <-
                JwtBearerDefaults.AuthenticationScheme
            options.DefaultChallengeScheme <-
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(fun options ->
            let audiences =
                [ configuration.["Auth0:Audience"]
                  configuration.["Auth0:Scheduled_Tasks_Audience"] ]
            options.Authority <-
                sprintf "https://%s" configuration.["Auth0:Issuer_Domain"]
            options.TokenValidationParameters <-
                TokenValidationParameters
                    (ValidateIssuer = false, ValidAudiences = audiences))
    |> ignore

    let extraEncoder =
        Extra.empty |> Extra.withInt64

    services.AddSingleton<Json.IJsonSerializer>
        (Thoth.Json.Giraffe.ThothSerializer
            (extra = extraEncoder, isCamelCase = true, skipNullField = true))
    |> ignore


[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")
    Migrate.Run(configuration.["ConnectionStrings:EventDb"])

    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureKestrel(fun _ options -> options.AllowSynchronousIO <- true)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0
