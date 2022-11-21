module AuthHandler

open System.Threading.Tasks
open System.Security.Claims
open Microsoft.AspNetCore.Authentication

type TestAuthHandlerOptions() =
    inherit AuthenticationSchemeOptions()
    member val BekkPermissions: string list = [] with get, set

type TestAuthHandler(options, logger, encoder, clock) =
    inherit AuthenticationHandler<TestAuthHandlerOptions>(options, logger, encoder, clock)
    let bekkPermissions = options.CurrentValue.BekkPermissions
    let claims = List.map (fun permission -> Claim("https://api.bekk.no/claims/permission", permission)) bekkPermissions
    member this.Scheme = "Test"
    // Todo: Send inn claims på no vis
    // Todo: Admin må kunne legges til og kunne testes der det er relevant
    override this.HandleAuthenticateAsync() =
        let claims = [ Claim(ClaimTypes.Name, "Test user")
                       Claim("https://api.bekk.no/claims/employeeId", "0")
                       yield! claims
                     ]
        let identity = ClaimsIdentity(claims, this.Scheme)
        let principal = ClaimsPrincipal(identity)
        let ticket = AuthenticationTicket(principal, this.Scheme)
        Task.FromResult(AuthenticateResult.Success(ticket))
