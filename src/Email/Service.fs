namespace ArrangementService.Email

open System.Text
open Giraffe
open FSharp.Data
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open ArrangementService
open Models
open SendgridApiModels

module Service =

    let private sendMailProd (options: SendgridOptions) (jsonBody: string) =
        let byteBody = UTF8Encoding().GetBytes(jsonBody)
        async {
            let! _ = Http.AsyncRequestString
                         (options.SendgridUrl, httpMethod = "POST",
                          headers =
                              [ "Authorization", (sprintf "Bearer %s" options.ApiKey)
                                "Content-Type", "application/json" ], body = BinaryUpload byteBody)
            ()
        }
        |> Async.Start

    let sendMail (email: Email) (context: HttpContext) =
        let sendgridConfig = context.GetService<SendgridOptions>()

        let mailFunction =
            if context.GetService<AppConfig>().isProd then sendMailProd sendgridConfig else printfn "%s"

        let serializerSettings =
            let settings = JsonSerializerSettings()
            settings.ContractResolver <- CamelCasePropertyNamesContractResolver()
            settings

        (emailToSendgridFormat email, serializerSettings)
        |> JsonConvert.SerializeObject
        |> mailFunction
