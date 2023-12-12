environment            = "dev"
sld_domain             = "bekk.dev"
hostname               = "skjer"
task_image             = "882089634282.dkr.ecr.eu-central-1.amazonaws.com/bekk-arrangement-svc"
task_image_tag         = "latest"
listener_path_patterns = ["/*"]
create_dns_record      = true

task_environment = {
  DOTNET_ENVIRONMENT                 = "Development"
  Auth0__Audience                    = "QHQy75S7tmnhDdBGYSnszzlhMPul0fAE"
  Auth0__Ansattlista_iOS_Audience    = "eS3wyVKSqWtLVXfDbFBRF7NJsbunXwwM"
  Auth0__Issuer_Domain               = "bekk.eu.auth0.com"
  Auth0__PERMISSION_CLAIM_TYPE       = "https://api.bekk.no/claims/permission"
  Config__Employee_Svc_url           = "https://api.bekk.dev/employee-svc"
  Sendgrid__Dev_White_List_Addresses = "hong.nhung.thi.vo@bekk.no, trond.tenfjord@bekk.no, ole.magnus.lie@bekk.no, gunvor.lemvik@bekk.no, ferdinand.loberg@bekk.no, mari.talmoen@bekk.no, frederik.heggenes@bekk.no"
}
task_secrets = [
  "ConnectionStrings__EventDb",
  "Sendgrid__Apikey",
  "OfficeEvents__TenantId",
  "OfficeEvents__Mailbox",
  "OfficeEvents__ClientId",
  "OfficeEvents__ClientSecret"
]
