# Retrieve vpc object
data "aws_vpc" "selected" {
  tags = {
    Name = "${var.base_name}-vpc-${var.environment}"
  }
}

resource "aws_lb_target_group" "main" {
  name        = "${var.base_name}-${var.app_name}-${var.environment}"
  port        = var.container_port
  protocol    = "HTTP"
  vpc_id      = data.aws_vpc.selected.id
  target_type = "ip"

  health_check {
    healthy_threshold   = "3"
    interval            = "6"
    protocol            = "HTTP"
    port                = "80"
    matcher             = "200"
    timeout             = "5"
    path                = var.task_health_check_path
    unhealthy_threshold = "2"
  }

  tags = {
    Environment = var.environment
    Terraform   = "true"
  }
}

# Retrieve load balancer listener
data "aws_lb_listener" "selected" {
  arn = var.aws_lb_listener_arn[var.environment]
}

# Add custom listener rule
resource "aws_lb_listener_rule" "main" {
  depends_on = [
    aws_lb_target_group.main
  ]
  listener_arn = data.aws_lb_listener.selected.arn
  priority     = 100

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.main.arn
  }

  condition {
    path_pattern {
      values = var.listener_path_patterns
    }
  }

  condition {
    host_header {
      values = ["${var.hostname}.${var.sld_domain}"]
    }
  }
}


# ECS task
data "aws_iam_role" "ecs_task_execution_role" {
  name = "${var.base_name}-ecsTaskExecutionRole"
}

data "aws_iam_role" "ecs_task_role" {
  name = "${var.base_name}-ecsTaskRole"
}

resource "aws_ecs_task_definition" "main" {
  family                   = "${var.base_name}-${var.app_name}-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.task_cpu
  memory                   = var.task_memory
  execution_role_arn       = data.aws_iam_role.ecs_task_execution_role.arn
  task_role_arn            = data.aws_iam_role.ecs_task_role.arn
  container_definitions = jsonencode([
    {
      name      = "${var.base_name}-${var.app_name}-${var.environment}"
      image     = "${var.ecr_endpoint}/${var.task_image}:${var.task_image_tag}"
      essential = true
      cpu       = 0
      portMappings = [
        {
          protocol      = "tcp"
          containerPort = var.container_port
          hostPort      = var.container_port
        }
      ]
      environment = var.container_environment
      mountPoints = []
      volumesFrom = []
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = "${var.base_name}-ecs-${var.environment}"
          awslogs-region        = var.aws_region
          awslogs-stream-prefix = "fargate"
        }
      }
    },
  ])
  tags = {
    Name      = "${var.base_name}-${var.app_name}-${var.environment}"
    Terraform = "true"
  }
}

data "aws_security_group" "selected" {
  tags = {
    Name = "${var.base_name}-sg-tasks-${var.environment}"
  }
}

data "aws_subnets" "private" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.selected.id]
  }

  tags = {
    Tier = "private"
  }
}

data "aws_ecs_cluster" "selected" {
  cluster_name = "${var.base_name}-ecs-cluster-${var.environment}"
}

resource "aws_ecs_service" "main" {
  name                               = "${var.base_name}-${var.app_name}-${var.environment}"
  cluster                            = data.aws_ecs_cluster.selected.id
  task_definition                    = aws_ecs_task_definition.main.arn
  desired_count                      = 1
  deployment_minimum_healthy_percent = 50
  deployment_maximum_percent         = 200
  launch_type                        = "FARGATE"
  scheduling_strategy                = "REPLICA"

  network_configuration {
    security_groups  = [data.aws_security_group.selected.id]
    subnets          = data.aws_subnets.private.ids
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.main.arn
    container_name   = "${var.base_name}-${var.app_name}-${var.environment}"
    container_port   = var.container_port
  }

  lifecycle {
    ignore_changes = [desired_count]
  }
}

data "aws_route53_zone" "selected" {
  name = var.sld_domain
}

data "aws_lb" "selected" {
  name = "bekk-lb-ecs-${var.environment}"
}

resource "aws_route53_record" "main" {
  zone_id = data.aws_route53_zone.selected.zone_id
  name    = "${var.hostname}.${data.aws_route53_zone.selected.name}"
  type    = "A"
  alias {
    name                   = data.aws_lb.selected.dns_name
    zone_id                = data.aws_lb.selected.zone_id
    evaluate_target_health = true
  }
}
