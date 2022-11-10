module AuthHandler

open System.Threading.Tasks
open System.Security.Claims
open Microsoft.AspNetCore.Authentication

type TestAuthHandler(options, logger, encoder, clock) =
    inherit AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
    member this.Scheme = "Test"
    override this.HandleAuthenticateAsync() =
        let claims = [ Claim(ClaimTypes.Name, "Test user")
                       Claim("https://api.bekk.no/claims/employeeId", "1437")
                     ]
        let identity = ClaimsIdentity(claims, this.Scheme)
        let principal = ClaimsPrincipal(identity)
        let ticket = AuthenticationTicket(principal, this.Scheme)
        Task.FromResult(AuthenticateResult.Success(ticket))
