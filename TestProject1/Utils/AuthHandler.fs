module AuthHandler

open System.Threading.Tasks
open System.Security.Claims
open Microsoft.AspNetCore.Authentication

type TestAuthHandler(options, logger, encoder, clock) =
    inherit AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
    member this.Scheme = "Test"
    // Todo: Send inn claims på no vis
    // Todo: Admin må kunne legges til og kunne testes der det er relevant
    override this.HandleAuthenticateAsync() =
        let claims = [ Claim(ClaimTypes.Name, "Test user")
                       Claim("https://api.bekk.no/claims/employeeId", "0")
                     ]
        let identity = ClaimsIdentity(claims, this.Scheme)
        let principal = ClaimsPrincipal(identity)
        let ticket = AuthenticationTicket(principal, this.Scheme)
        Task.FromResult(AuthenticateResult.Success(ticket))
