terraform {
  backend "s3" {
    region     = "eu-central-1"
    bucket     = "bekk-terraform-app-states"
    profile    = "deploy"
    key        = "bekk-arrangement-svc.tfstate"
    kms_key_id = "870a3c58-7201-4334-8c32-b257d38e9a12"
    encrypt    = true
    # Table to store lock in
    dynamodb_table = "bekk-terraform-state-lock-apps"
  }
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.5"
    }
  }
}

provider "aws" {
  region  = var.aws_region
  profile = "deploy"
}

module "aws-deploy" {
  source                 = "git@github.com:bekk/bekk-terraform-aws-deploy.git"
  base_name              = "bekk"
  app_name               = "arrangement-svc"
  aws_region             = var.aws_region
  environment            = var.environment
  preview_name           = var.preview_name
  hostname               = var.hostname
  sld_domain             = var.sld_domain
  create_dns_record      = var.create_dns_record
  listener_path_patterns = var.listener_path_patterns
  task_image             = var.task_image
  task_image_tag         = var.task_image_tag
  task_environment       = var.task_environment
  task_secrets           = var.task_secrets
  enable_tracing         = true
  memory_size            = 512
}

output "URL" {
  value = module.aws-deploy.url
}
