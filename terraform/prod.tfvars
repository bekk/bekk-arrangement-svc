environment            = "prod"
sld_domain             = "bekk.no"
hostname               = "skjer"
task_image             = "882089634282.dkr.ecr.eu-central-1.amazonaws.com/arrangement-svc"
task_image_tag         = "latest"
task_cpu               = 512
task_memory            = 1024
listener_path_patterns = ["/*"]
create_dns_record      = true
task_environment = {
  DOTNET_ENVIRONMENT                 = "Production"
  Auth0__Audience                    = "HuH7oGHSgymn4mYLzEClyE2bhQSM1iTC"
  Auth0__Issuer_Domain               = "bekk.eu.auth0.com"
  Auth0__PERMISSION_CLAIM_TYPE       = "https://api.bekk.no/claims/permission"
  Auth0__Scheduled_Tasks_Audience    = "https://api.bekk.no"
  Config__Employee_Svc_url           = "https://api.bekk.no/employee-svc"
  Sendgrid__Dev_White_List_Addresses = ""
}
task_secrets = [
  "ConnectionStrings__EventDb",
  "Sendgrid__Apikey",
  "OfficeEvents__TenantId",
  "OfficeEvents__Mailbox",
  "OfficeEvents__ClientId",
  "OfficeEvents__ClientSecret"
]
