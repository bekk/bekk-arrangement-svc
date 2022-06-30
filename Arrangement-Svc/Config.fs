module Config

open Giraffe
open System.Data
open Microsoft.AspNetCore.Http

type AppConfig =
    { isProd: bool
      userIdClaimsKey: string
      permissionsAndClaimsKey: string
      adminPermissionClaim: string
      readPermissionClaim: string
      sendMailInDevEnvWhiteList: string list
      noReplyEmail: string
      databaseConnectionString: string
    }