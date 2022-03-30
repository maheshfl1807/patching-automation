terraform {
  backend "s3" {
    bucket  = "2w-pe-terraform"
    key     = "server-report-service/dev/terraform.tfstate"
    region  = "us-west-2"
    encrypt = true
    acl     = "bucket-owner-full-control"
  }
}

data "terraform_remote_state" "dev" {
  backend = "s3"
  config = {
    bucket = "2w-pe-terraform"
    key    = "dev/terraform.tfstate"
    region = "us-west-2"
  }
}

provider "aws" {
  region              = local.aws_provider_region
  profile             = local.aws_provider_profile
  allowed_account_ids = ["061165946885"]
}

locals {
  env = "dev-eng"
  aws_provider_region = "us-west-2"
  aws_provider_profile = "default"
  eks_oidc_provider_arn = "arn:aws:iam::061165946885:oidc-provider/oidc.eks.us-west-2.amazonaws.com/id/62E18F7F4B5A53189D7DE1E83EB3148B"
  eks2_oidc_provider_arn = "arn:aws:iam::061165946885:oidc-provider/oidc.eks.us-west-2.amazonaws.com/id/314643C241F99EDD32E923193EB60033"
  vpc_id = data.terraform_remote_state.dev.outputs.module_vpc_vpc_id
  main_region = "us-west-2"
}

module "shared" {
  source = "../shared"
  providers = {
    aws = aws
  }
  env = local.env
  eks_oidc_provider_arn = local.eks_oidc_provider_arn
  eks2_oidc_provider_arn = local.eks2_oidc_provider_arn
  vpc_id = local.vpc_id
  main_region = local.main_region
}