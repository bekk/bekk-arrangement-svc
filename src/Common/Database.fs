namespace ArrangementService

open Dapper.FSharp.MSSQL
open Microsoft.AspNetCore.Http
open System.Data
open System.Data.SqlClient

open Config

module Database =

    (*
        `createConnection` is idempotent, i.e. you can call it many times
        and still get the same transaction.

        This is useful because one (http) request does potentially many
        "check"s before doing a "handle" of the endpoint. You really want
        the "are there available spots on Event" to be evaluated in the
        same transaction as the "add Participant to Event" operation.
    *)
    let createConnection (ctx: HttpContext) = 
        let config = getConfig ctx
        if config.currentTransaction <> null then
            config.currentConnection, config.currentTransaction
        else
            let connection = new SqlConnection(config.databaseConnectionString) :> IDbConnection
            connection.Open()
            config.currentConnection <- connection
            let transaction = connection.BeginTransaction(IsolationLevel.Serializable)
            config.currentTransaction <- transaction
            connection, transaction

    let runSelectQuery<'t> (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.SelectAsync<'t>(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runUpdateQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.UpdateAsync(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runDeleteQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.DeleteAsync(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runInsertQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.InsertAsync(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore
        // TODO: Sjekk retur verdi og returner basert på det

        Ok ()