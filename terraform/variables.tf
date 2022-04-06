variable "aws_region" {
  type    = string
  default = "eu-central-1"
}

variable "base_name" {
  type    = string
  default = "bekk"
}

variable "environment" {
  type    = string
  default = "dev"
}

variable "sld_domain" {
  type    = string
  default = "bekk.dev"
}
variable "app_name" {
  type = string
}

variable "hostname" {
  type = string
}

variable "ecr_endpoint" {
  type = string
}

variable "task_image" {
  type = string
}

variable "task_image_tag" {
  type = string
}

variable "task_cpu" {
  type    = number
  default = 256
}

variable "task_memory" {
  type    = number
  default = 512
}

variable "listener_path_patterns" {
  type = list(string)
}

variable "container_environment" {
  type = list(map(string))
}

variable "task_secrets" {
  type = map(string)
  sensitive = true
}
