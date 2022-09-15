environment            = "dev"
sld_domain             = "bekk.dev"
hostname               = "skjer"
task_image             = "882089634282.dkr.ecr.eu-central-1.amazonaws.com/bekk-arrangement-svc"
task_image_tag         = "latest"
listener_path_patterns = ["/*"]
create_dns_record      = true

task_environment = {
  ASPNETCORE_ENVIRONMENT       = "Development"
  Auth0__Audience              = "QHQy75S7tmnhDdBGYSnszzlhMPul0fAE"
  Auth0__Issuer_Domain         = "bekk-dev.eu.auth0.com"
  Auth0__PERMISSION_CLAIM_TYPE = "https://api.bekk.no/claims/permission"
  Config__Employee_Svc_url     = "https://api.bekk.dev/employee-svc"
  Serilog__MinimumLevel        = "Warning"
  VIRTUAL_PATH                 = "/arrangement-svc"
}
task_secrets = [
  "ConnectionStrings__EventDb",
  "Sendgrid__Apikey",
  "OfficeEvents__TenantId",
  "OfficeEvents__Mailbox",
  "OfficeEvents__ClientId",
  "OfficeEvents__ClientSecret"
]