# Import Service
Imports servers from cloud providers into MCS patching tool.

## Start services
docker-compose up -d

## Attach to logs
docker-compose logs -f

## Create bootstrap migrations and set up db
DROP DATABASE importservice;
del Migrations -r
dotnet ef migrations add Bootstrap
dotnet ef database update