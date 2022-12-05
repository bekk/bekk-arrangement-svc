module Database

open Microsoft.Data.SqlClient

open Utils

let private createConnectionString (rawConnectionString: string) : string =
    let cs = SqlConnectionStringBuilder(rawConnectionString)
    cs.InitialCatalog <- ""
    cs.ConnectionString

let create connectionString : unit =
    if Container.containerIsStopped() then failwith "Cannot create database, container not running."
    use connection = new SqlConnection(createConnectionString connectionString)
    connection.Open()
    use command = connection.CreateCommand()
    command.CommandText <- "CREATE DATABASE [arrangement-db];"
    command.ExecuteNonQuery() |> ignore
    connection.Close()

let migrate connectionString : unit =
    printfn "Migrating database"
    migrator.Migrate.Run(createConnectionString(connectionString))
