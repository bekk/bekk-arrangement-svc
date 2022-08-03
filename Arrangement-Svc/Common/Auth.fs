module Auth

open System
open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Http

open Config

let employeeIdClaim = "https://api.bekk.no/claims/employeeId"

let isAuthenticated (next: HttpFunc) (context: HttpContext)  =
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme) next context

let isAuthenticatedf f = (fun x -> isAuthenticated >=> (f x))

let isAdmin (context: HttpContext) =
    let config = context.GetService<AppConfig>()
    context.User.HasClaim(config.permissionsAndClaimsKey, config.adminPermissionClaim)

let getUserId (context: HttpContext) =
    context.User.FindFirst(employeeIdClaim)
    |> Option.ofObj
    |> Option.map (fun c -> c.Value)
    |> Option.bind (Int32.TryParse >> (function | true, x -> Some x | false, _ -> None))
