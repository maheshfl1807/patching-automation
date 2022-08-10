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

variable "msk_cluster_name" {
  description = "Name of the MSK cluster."
}

variable "msk_cluster_id" {
  description = "Identifier of the MSK cluster."
}

variable "tags_as_map" {
  description = "General tags to add to resources."
  type        = map(string)
  default     = {}
}

variable "mcs_tags_as_map" {
  description = "MCS-specific tags to add to resources."
  type        = map(string)
  default     = {}
}