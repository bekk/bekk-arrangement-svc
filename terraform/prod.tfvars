base_name              = "bekk"
environment            = "prod"
sld_domain             = "bekk.no"
hostname               = "skjer"
app_name               = "arrangement-svc"
task_image             = "nginx"
task_image_tag         = "latest"
listener_path_patterns = ["/api", "/api/*"]
container_environment = [
  {
    "name"  = "ASPNETCORE_ENVIRONMENT"
    "value" = "Production"
  },
  {
    "name"  = "Auth0__Issuer_Domain"
    "value" = "bekk.eu.auth0.com"
  },
  {
    "name"  = "Auth0__Audience"
    "value" = "QHQy75S7tmnhDdBGYSnszzlhMPul0fAE"
  },
  {
    "name"  = "PORT"
    "value" = "80"
  },
  {
    "name"  = "Logging__Console__LogLevel__Default"
    "value" = "Warning"
  },
  {
    "name"  = "Serilog__MinimumLevel"
    "value" = "Warning"
  },
  {
    "name"  = "VIRTUAL_PATH"
    "value" = "/arrangement-svc"
  },
  {
    "name"  = "Auth0__PERMISSION_CLAIM_TYPE"
    "value" = "https://api.bekk.no/claims/permission"
  }
]
