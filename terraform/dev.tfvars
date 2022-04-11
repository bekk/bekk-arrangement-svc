base_name              = "bekk"
environment            = "dev"
sld_domain             = "bekk.dev"
hostname               = "api"
app_name               = "arrangement-svc"
task_image             = "882089634282.dkr.ecr.eu-central-1.amazonaws.com/arrangement-svc"
task_image_tag         = "38"
listener_path_patterns = ["/arrangement-svc*"]
container_environment = [
  {
    "name"  = "ASPNETCORE_ENVIRONMENT"
    "value" = "Development"
  },
  {
    "name"  = "Auth0__Issuer_Domain"
    "value" = "bekk-dev.eu.auth0.com"
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
