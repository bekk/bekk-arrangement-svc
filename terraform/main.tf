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

locals {
  secrets = [
    {
      "name" : "Sendgrid__Apikey",
      "value" : var.Sendgrid__Apikey
    },
    {
      "name": "ConnectionStrings__EventDb",
      "value": var.ConnectionStrings__EventDb
    }
  ]
  container_environment = concat(var.container_environment, local.secrets)
}

module "aws-deploy" {
  source                 = "./aws-deploy"
  aws_region             = var.aws_region
  base_name              = var.base_name
  environment            = var.environment
  app_name               = var.app_name
  hostname               = var.hostname
  sld_domain             = var.sld_domain
  listener_path_patterns = var.listener_path_patterns
  ecr_endpoint           = var.ecr_endpoint
  task_image             = var.task_image
  task_image_tag         = var.task_image_tag
  container_environment  = local.container_environment
}

