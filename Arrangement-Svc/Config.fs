module Config

open System

let runPodman = isNull <| Environment.GetEnvironmentVariable("NO_PODMAN")
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
