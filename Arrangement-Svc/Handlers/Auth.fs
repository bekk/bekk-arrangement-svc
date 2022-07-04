module AuthHandler

open Giraffe
open Microsoft.AspNetCore.Http

type Config = {
    EmployeeSvcUrl: string
    Audience: string
    IssuerDomain: string
    Scopes: string
}
let configHandler (next: HttpFunc) (context: HttpContext) =
    let config = context.GetService<Config>()
    json config next context
let config: HttpHandler =
    route "/api/config" >=> configHandler
