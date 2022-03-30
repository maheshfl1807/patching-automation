variable "env" {
  description = "The environment that resources will be utilized in."
}

variable "eks_oidc_provider_arn" {
  description = "Primary EKS cluster OIDC provider ARN."
}

variable "eks2_oidc_provider_arn" {
  description = "Secondary EKS cluster OIDC provider ARN."
}

variable "vpc_id" {
  description = "VPC identifier to add resources to."
}

variable "main_region" {
  description = "Region where resources will be created."
}