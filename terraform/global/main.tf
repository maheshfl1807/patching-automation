terraform {
  backend "s3" {
    bucket  = "2w-pe-terraform"
    key     = "server-report-service/global/terraform.tfstate"
    region  = "us-west-2"
    encrypt = true
    acl     = "bucket-owner-full-control"
  }
}


locals {
  env = "global"
}