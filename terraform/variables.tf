variable "aws_region" {
  type    = string
  default = "eu-central-1"
}

variable "environment" {
  type    = string
  default = "dev"
}

variable "preview_name" {
  type    = string
  default = ""
}

variable "sld_domain" {
  type    = string
  default = "bekk.dev"
}

variable "create_dns_record" {
  type    = bool
  default = false
}

variable "hostname" {
  type = string
}

variable "task_image" {
  type = string
}

variable "task_image_tag" {
  type    = string
  default = "latest"
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

variable "task_secrets" {
  type = list(string)
}

variable "task_environment" {
  type = map(string)
}
