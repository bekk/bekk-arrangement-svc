[<AutoOpen>]
module DatabaseContext

open System
open Microsoft.Data.SqlClient
open Microsoft.AspNetCore.Http
open Giraffe

type DatabaseContext(cn : SqlConnection, tx : SqlTransaction) =
    do
        if not (isNull tx) then assert (tx.Connection = cn)
    let mutable disposed = false
    let mutable txComplete = false
    member _.Connection = cn
    member _.Transaction = tx

    member _.Commit () =
        assert (not txComplete)
        tx.Commit()
        txComplete <- true

    member _.Rollback () =
        assert (not txComplete)
        tx.Rollback()
        txComplete <- true

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                if not (isNull tx) then
                    if (not txComplete) then
                        tx.Rollback()
                    tx.Dispose()
                if not (isNull cn) then
                    cn.Close()
                    cn.Dispose()
                disposed <- true


let openConnection (ctx : HttpContext) =
    let cn = ctx.GetService<SqlConnection>()
    cn.Open()
    new DatabaseContext(cn, null)

let openTransaction (ctx: HttpContext) =
    let cn = ctx.GetService<SqlConnection>()
    cn.Open()
    let tx = cn.BeginTransaction()
    new DatabaseContext(cn, tx)
