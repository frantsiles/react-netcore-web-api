FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder
WORKDIR /src

COPY src/Gateway/Gateway.Api/Gateway.Api.csproj src/Gateway/Gateway.Api/
RUN dotnet restore src/Gateway/Gateway.Api/Gateway.Api.csproj

COPY src/Gateway/ src/Gateway/
RUN dotnet publish src/Gateway/Gateway.Api/Gateway.Api.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=builder --chown=app:app /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
