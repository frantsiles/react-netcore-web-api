FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder
WORKDIR /src

COPY src/Api/Api.Domain/Api.Domain.csproj           src/Api/Api.Domain/
COPY src/Api/Api.Application/Api.Application.csproj src/Api/Api.Application/
COPY src/Api/Api.Infrastructure/Api.Infrastructure.csproj src/Api/Api.Infrastructure/
COPY src/Api/Api.WebApi/Api.WebApi.csproj           src/Api/Api.WebApi/
RUN dotnet restore src/Api/Api.WebApi/Api.WebApi.csproj

COPY src/Api/ src/Api/
RUN dotnet publish src/Api/Api.WebApi/Api.WebApi.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=builder --chown=app:app /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -q --spider http://localhost:8080/api/health || exit 1
ENTRYPOINT ["dotnet", "Api.WebApi.dll"]
