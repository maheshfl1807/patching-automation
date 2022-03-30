terraform {
  backend "s3" {
    bucket  = "2w-pe-terraform"
    key     = "server-report-service/prod/terraform.tfstate"
    region  = "us-west-2"
    encrypt = true
    acl     = "bucket-owner-full-control"
  }
}

provider "aws" {
  region              = local.aws_provider_region
  profile             = local.aws_provider_profile
  allowed_account_ids = ["061165946885"]
}

locals {
  env = "prod-eng"
  aws_provider_region = "us-west-2"
  aws_provider_profile = "default"
  eks_oidc_provider_arn = "arn:aws:iam::061165946885:oidc-provider/oidc.eks.us-west-2.amazonaws.com/id/22143DBD43E60E45091475AE1D4C0160"
  eks2_oidc_provider_arn = "arn:aws:iam::061165946885:oidc-provider/oidc.eks.us-west-2.amazonaws.com/id/4061E2E752BD003CFA7095AE9C92C60C"
}

module "shared" {
  source = "../shared"
  providers = {
    aws = aws
  }
  env = "prod"
  eks_oidc_provider_arn = local.eks_oidc_provider_arn
  eks2_oidc_provider_arn = local.eks2_oidc_provider_arn
}