namespace ArrangementService

open FSharp.Data.Sql

module Database =
    [<Literal>]
    let ConnectionString =
        "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"

    [<Literal>]
    let DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER

    [<Literal>]
    let ResolutionPath = "~/.nuget/packages/system.data.sqlclient/4.7.0/lib/netcoreapp2.1"

    type ArrangementSql = SqlDataProvider<ContextSchemaPath="DbSchema.json", ConnectionString=ConnectionString, DatabaseVendor=DatabaseVendor, ResolutionPath=ResolutionPath>

    type ArrangementDbContext = ArrangementSql.dataContext

    let createDbContext (connectionString: string): ArrangementSql.dataContext =
        ArrangementSql.GetDataContext(connectionString)
