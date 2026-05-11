FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder
WORKDIR /src

COPY src/BFF/BFF.Domain/BFF.Domain.csproj               src/BFF/BFF.Domain/
COPY src/BFF/BFF.Application/BFF.Application.csproj     src/BFF/BFF.Application/
COPY src/BFF/BFF.Infrastructure/BFF.Infrastructure.csproj src/BFF/BFF.Infrastructure/
COPY src/BFF/BFF.Api/BFF.Api.csproj                     src/BFF/BFF.Api/
RUN dotnet restore src/BFF/BFF.Api/BFF.Api.csproj

COPY src/BFF/ src/BFF/
RUN dotnet publish src/BFF/BFF.Api/BFF.Api.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=builder --chown=app:app /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:8080/bff/health || exit 1
ENTRYPOINT ["dotnet", "BFF.Api.dll"]
