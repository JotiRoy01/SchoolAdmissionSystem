# SchoolAdmissionSystem
This project is given by onnorokom projukti

# run dotnet
```bash
dotnet run --no-launch-profile --project backend/SchoolAdmission.Api --urls http://localhost:8080
```
# To specify which project are registered
```bash
dotnet sln list
```
# PostgreSQL provider
```bash
dotnet add backend/SchoolAdmission.Api package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
```
# EF Core Design
```bash
dotnet add backend/SchoolAdmission.Api package Microsoft.EntityFrameworkCore.Design --version 10.0.14
```
# EF Core CLI
```bash
dotnet tool install --global dotnet-ef --version 10.0.14
```
# Varify Dependencies
```bash
dotnet ef --version
and
dotnet list backend/SchoolAdmission.Api package
```



