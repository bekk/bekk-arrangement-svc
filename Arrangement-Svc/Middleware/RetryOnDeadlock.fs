namespace Middleware

open System

open Microsoft.AspNetCore.Http
open Microsoft.Data.SqlClient

open Microsoft.Extensions.Logging
open Polly

type RetryOnDeadlock(next: RequestDelegate) =
    member this.Invoke(ctx: HttpContext, logger: ILogger<RetryOnDeadlock>) =
        Policy
            .Handle<SqlException>(fun ex ->
                let wasDeadlocked =
                    [ "was deadlocked"
                      "chosen as the deadlock victim"
                      "Rerun the transaction"
                    ]
                    |> Seq.forall ex.Message.Contains
                if wasDeadlocked
                then logger.LogError(ex, "Query deadlocked and killed. Retrying.")
                else logger.LogError(ex, "SqlException, but not deadlocked")
                wasDeadlocked)
            .WaitAndRetryAsync(3, fun rc -> TimeSpan.FromSeconds(2.0 ** rc))
            .ExecuteAsync(fun () -> next.Invoke(ctx))
