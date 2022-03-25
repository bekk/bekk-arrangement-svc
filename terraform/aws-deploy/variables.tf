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

variable "task_health_check_path" {
  type    = string
  default = "/health"
}

variable "aws_lb_listener_arn" {
  type = map(string)
  default = {
    "dev"  = "arn:aws:elasticloadbalancing:eu-central-1:882089634282:listener/app/bekk-lb-ecs-dev/0252314628537d69/1aaae3420eaade33"
    "prod" = "not ready yet"
  }
}

variable "listener_path_patterns" {
  type    = list(string)
  default = ["/*"]
}

variable "container_port" {
  type    = number
  default = 80
}

variable "container_environment" {
  type = list(map(string))
}
