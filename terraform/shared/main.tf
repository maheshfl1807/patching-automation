data "aws_caller_identity" "current" {
  provider = aws
}

locals {
  vpc_access_point_name = "${var.env}-server-report-service-reports"
}

module "report_bucket" {
  source = "github.com/2ndWatch/pe-terraform-aws-s3.git?ref=v6.5.1"

  providers = {
    aws.main    = aws
    aws.replica = aws
  }

  enable                                = true
  enable_kms                            = true
  unique_bucket_name                    = "${var.env}-server-report-service-reports"
  create_main_accesspoint_bucket_policy = true
  tags                                  = merge(var.tags_as_map, var.mcs_tags_as_map, { "2W_Workload" = "server_reports" })

  main_lifecycle_rules = [
    {
      id      = "base"
      enabled = true
      prefix  = ""
      expiration = {
        days = 30
      }
      noncurrent_version_expiration = {
        days = 7
      }
    }
  ]
}

module "service_role" {
  source = "github.com/2ndWatch/pe-terraform-aws-iam.git?ref=v8.1.0"
  providers = {
    aws = aws
  }

  name_prefix = var.env
  role_name   = "server-report-service"
  role_assume_policy_configs = [
    {
      principal_type  = "Federated"
      principal_value = [var.eks2_oidc_provider_arn, var.eks_oidc_provider_arn]
      action          = "sts:AssumeRoleWithWebIdentity"
    }
  ]
  tags                         = merge(var.tags_as_map, var.mcs_tags_as_map, { "2W_Workload" = "server_reports" })
  role_inline_policy_statement = <<EOF
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "",
            "Effect": "Allow",
            "Action":"sts:AssumeRole",
            "Resource":"arn:aws:iam::536269885160:role/2WPlatformAutomationAssumeRoleRole"
        },
        {
            "Sid": "",
            "Effect": "Allow",
            "Action": [
                "s3:List*",
                "s3:GetObject*",
                "s3:PutObject*",
                "s3:DeleteObject*"
            ],
            "Resource": [
              "arn:aws:s3:${var.main_region}:${data.aws_caller_identity.current.account_id}:accesspoint/${local.vpc_access_point_name}",
              "arn:aws:s3:${var.main_region}:${data.aws_caller_identity.current.account_id}:accesspoint/${local.vpc_access_point_name}/*"
            ]
        },
        {
            "Sid": "",
            "Effect": "Allow",
            "Action": [
                "kms:DescribeKey",
                "kms:Encrypt",
                "kms:Decrypt",
                "kms:GenerateDataKey*"
            ],
            "Resource": "${module.report_bucket.main_kms_arn}"
        },
        {
            "Effect": "Allow",
            "Action": [
                "sns:Publish"
            ],
            "Resource": [
                "${aws_sns_topic.report_topic.arn}"
            ]
        }
    ]
}
EOF
}

resource "aws_s3_access_point" "report_access_point" {
  provider = aws
  bucket   = module.report_bucket.main_bucket_name
  name     = local.vpc_access_point_name
  policy   = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "VpcAccessPointRWD",
      "Effect": "Allow",
      "Principal": {
          "AWS": [
            "${module.service_role.role_arn}"
          ]
      },
      "Action": [
          "s3:List*",
          "s3:GetObject*",
          "s3:PutObject*",
          "s3:DeleteObject*"
      ],
      "Resource": [
        "arn:aws:s3:${var.main_region}:${data.aws_caller_identity.current.account_id}:accesspoint/${local.vpc_access_point_name}/object/*"
      ]
    }
  ]
}
EOF
}

resource "aws_sns_topic" "report_topic" {
  name = "${var.env}-server-report-service-server-report"
}
