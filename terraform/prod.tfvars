environment            = "prod"
sld_domain             = "bekk.no"
hostname               = "skjer"
task_image             = "882089634282.dkr.ecr.eu-central-1.amazonaws.com/arrangement-svc"
task_image_tag         = "latest"
listener_path_patterns = ["/*"]
create_dns_record      = true
task_environment = {
  ASPNETCORE_ENVIRONMENT          = "Production"
  Auth0__Audience                 = "HuH7oGHSgymn4mYLzEClyE2bhQSM1iTC"
  Auth0__Issuer_Domain            = "bekk.eu.auth0.com"
  Auth0__PERMISSION_CLAIM_TYPE    = "https://api.bekk.no/claims/permission"
  Auth0__Scheduled_Tasks_Audience = "https://api.bekk.no"
  Config__Employee_Svc_url        = "https://api.bekk.no/employee-svc"
  Serilog__MinimumLevel           = "Warning"
  VIRTUAL_PATH                    = "/arrangement-svc"
}
task_secrets = [
  "ConnectionStrings__EventDb",
  "Sendgrid__Apikey",
  "OfficeEvents__TenantId",
  "OfficeEvents__Mailbox",
  "OfficeEvents__ClientId",
  "OfficeEvents__ClientSecret"
]