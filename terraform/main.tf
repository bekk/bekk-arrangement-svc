terraform {
  backend "s3" {
    region                  = "eu-central-1"
    bucket                  = "bekk-terraform-states"
    shared_credentials_file = "~/.aws/creds"
    key                     = "bekk-arrangement-svc.tfstate"
    kms_key_id              = "063512b0-1833-462b-9250-f1c080d09c63"
    encrypt                 = true
    # Table to store lock in
    dynamodb_table = "bekk-terraform-state-lock"
  }
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.5"
    }
  }
}

provider "aws" {
  shared_credentials_files = ["~/.aws/creds"]
  region                   = var.aws_region
}

locals {
  container_environment = concat(var.container_environment, var.container_secrets)
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

