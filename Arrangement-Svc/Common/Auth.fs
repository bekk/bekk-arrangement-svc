module Auth

open System
open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Http

open Config

let employeeIdClaim = "https://api.bekk.no/claims/employeeId"

let isAuthenticated (next: HttpFunc) (context: HttpContext)  =
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme) next context

let isAdmin (context: HttpContext) =
    let config = context.GetService<AppConfig>()
    context.User.HasClaim(config.permissionsAndClaimsKey, config.adminPermissionClaim)

let getUserId (context: HttpContext) =
    task {
        let value = context.User.FindFirst(employeeIdClaim)
        return
            if value = null then
                None
            else
                let parsedSuccessfully, parsedValue = Int32.TryParse(value.Value)
                if parsedSuccessfully then
                    Some parsedValue
                else
                    None
    }
