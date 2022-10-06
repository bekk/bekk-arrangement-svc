module Config

open System

let manageContainers = isNull <| Environment.GetEnvironmentVariable("NO_CONTAINER_MANAGEMENT")
let runMigration = isNull <| Environment.GetEnvironmentVariable("NO_MIGRATION")

type AppConfig =
    { isProd: bool
      userIdClaimsKey: string
      permissionsAndClaimsKey: string
      adminPermissionClaim: string
      readPermissionClaim: string
      sendMailInDevEnvWhiteList: string list
      noReplyEmail: string
    }
