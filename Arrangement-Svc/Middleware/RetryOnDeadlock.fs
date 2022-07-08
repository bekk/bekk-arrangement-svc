namespace Middleware

open System

open Microsoft.AspNetCore.Http
open Microsoft.Data.SqlClient

open Polly

open Bekk.Canonical.Logger

type RetryOnDeadlock(next: RequestDelegate) =
    member this.Invoke(ctx: HttpContext, logger: Logger) =
        Policy
            .Handle<SqlException>(fun ex ->
                [ "was deadlocked"
                  "chosen as the deadlock victim"
                  "Rerun the transaction"
                ]
                |> Seq.forall ex.Message.Contains)
            // TODO: Add logging
            .WaitAndRetryAsync(3, fun rc -> TimeSpan.FromSeconds(2.0 ** rc))
            .ExecuteAsync(fun () -> next.Invoke(ctx))
