environment            = "prod"
sld_domain             = "bekk.no"
hostname               = "skjer"
task_image             = "882089634282.dkr.ecr.eu-central-1.amazonaws.com/bekk-arrangement"
task_image_tag         = "latest"
listener_path_patterns = ["/*"]
create_dns_record      = true
task_environment = {
  ASPNETCORE_ENVIRONMENT       = "Production"
  Auth0__Issuer_Domain         = "bekk.eu.auth0.com"
  Auth0__Audience              = "QHQy75S7tmnhDdBGYSnszzlhMPul0fAE"
  PORT                         = "80"
  Serilog__MinimumLevel        = "Warning"
  Auth0__PERMISSION_CLAIM_TYPE = "https://api.bekk.no/claims/permission"
  EMPLOYEE_SVC_URL             = "https://api.bekk.no/employee-svc"
}
task_secrets = [
  "ConnectionStrings__EventDb",
  "Sendgrid__Apikey",
  "OfficeEvents__TenantId",
  "OfficeEvents__Mailbox",
  "OfficeEvents__ClientId",
  "OfficeEvents__ClientSecret"
]
