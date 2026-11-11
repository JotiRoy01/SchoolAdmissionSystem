FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/SchoolAdmission.Api/SchoolAdmission.Api.csproj backend/SchoolAdmission.Api/
RUN dotnet restore backend/SchoolAdmission.Api/SchoolAdmission.Api.csproj
COPY backend/SchoolAdmission.Api/ backend/SchoolAdmission.Api/
RUN dotnet publish backend/SchoolAdmission.Api/SchoolAdmission.Api.csproj \
    --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
