module Config

open Giraffe
open System.Data
open Microsoft.AspNetCore.Http

type AppConfig =
    { isProd: bool
      requestId: string
      userIdClaimsKey: string
      permissionsAndClaimsKey: string
      adminPermissionClaim: string
      readPermissionClaim: string
      sendMailInDevEnvWhiteList: string list
      noReplyEmail: string
      databaseConnectionString: string
      mutable currentConnection: IDbConnection
      mutable currentTransaction: IDbTransaction
      mutable log: (string * string) seq
    }
    
let getConfig (context: HttpContext) =
    context.GetService<AppConfig>()



