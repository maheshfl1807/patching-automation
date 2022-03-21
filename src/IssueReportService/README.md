# Import Service
Imports servers from cloud providers into MCS patching tool.

## Create bootstrap migrations and set up db
DROP DATABASE issuereportservice;
del Migrations -r
dotnet ef migrations add Bootstrap
dotnet ef database update