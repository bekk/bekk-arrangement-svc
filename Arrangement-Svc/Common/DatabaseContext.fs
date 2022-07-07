[<AutoOpen>]
module DatabaseContext

open System
open Microsoft.Data.SqlClient
open Microsoft.AspNetCore.Http
open Giraffe

type DatabaseContext(cn : SqlConnection, tx : SqlTransaction) =
    do
        assert (isNotNull cn)
        if isNotNull tx then assert (tx.Connection = cn)
    let mutable isDisposed = false
    member _.Connection = cn
    member _.Transaction = tx

    member this.Commit () =
        tx.Commit()

    member _.Rollback () =
        tx.Rollback()

    member private this.Dispose(disposing: bool) =
        if isDisposed then ()
        else
        // NOTE: Do we care if it's been called only using the finalizer (disposing=false)? It means it has been used incorrectly, but
        // we should still clean up even though it's a bit late.

        if isNotNull tx then
            tx.Dispose()
        cn.Dispose()
        isDisposed <- true

    override this.Finalize() =
        this.Dispose(false)

    interface IDisposable with
        member this.Dispose() =
            this.Dispose(true)
            GC.SuppressFinalize(this)


let openConnection (ctx : HttpContext) =
    let cn = ctx.GetService<SqlConnection>()
    cn.Open()
    new DatabaseContext(cn, null)

let openTransaction (ctx: HttpContext) =
    let cn = ctx.GetService<SqlConnection>()
    cn.Open()
    let tx = cn.BeginTransaction()
    new DatabaseContext(cn, tx)
