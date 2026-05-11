FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder
WORKDIR /src

COPY src/Worker/Worker.Service/Worker.Service.csproj src/Worker/Worker.Service/
RUN dotnet restore src/Worker/Worker.Service/Worker.Service.csproj

COPY src/Worker/ src/Worker/
RUN dotnet publish src/Worker/Worker.Service/Worker.Service.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=builder --chown=app:app /app/publish .
ENTRYPOINT ["dotnet", "Worker.Service.dll"]
