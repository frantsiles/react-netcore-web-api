FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

# ── csproj files for layer-cached restore ────────────────────────────────────
COPY src/Shared/Shared.Messages/Shared.Messages.csproj src/Shared/Shared.Messages/
COPY src/Api/Api.Domain/Api.Domain.csproj               src/Api/Api.Domain/
COPY src/Api/Api.Application/Api.Application.csproj     src/Api/Api.Application/
COPY src/Api/Api.Infrastructure/Api.Infrastructure.csproj src/Api/Api.Infrastructure/
COPY src/Api/Api.WebApi/Api.WebApi.csproj               src/Api/Api.WebApi/

COPY src/Parties/Parties.Domain/Parties.Domain.csproj                   src/Parties/Parties.Domain/
COPY src/Parties/Parties.Application/Parties.Application.csproj         src/Parties/Parties.Application/
COPY src/Parties/Parties.Infrastructure/Parties.Infrastructure.csproj   src/Parties/Parties.Infrastructure/

COPY src/Catalog/Catalog.Domain/Catalog.Domain.csproj                   src/Catalog/Catalog.Domain/
COPY src/Catalog/Catalog.Application/Catalog.Application.csproj         src/Catalog/Catalog.Application/
COPY src/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj   src/Catalog/Catalog.Infrastructure/

COPY src/Sales/Sales.Domain/Sales.Domain.csproj                         src/Sales/Sales.Domain/
COPY src/Sales/Sales.Application/Sales.Application.csproj               src/Sales/Sales.Application/
COPY src/Sales/Sales.Infrastructure/Sales.Infrastructure.csproj         src/Sales/Sales.Infrastructure/

COPY src/Inventory/Inventory.Domain/Inventory.Domain.csproj             src/Inventory/Inventory.Domain/
COPY src/Inventory/Inventory.Application/Inventory.Application.csproj   src/Inventory/Inventory.Application/
COPY src/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj src/Inventory/Inventory.Infrastructure/

COPY src/ControlPlane/ControlPlane.Domain/ControlPlane.Domain.csproj               src/ControlPlane/ControlPlane.Domain/
COPY src/ControlPlane/ControlPlane.Application/ControlPlane.Application.csproj     src/ControlPlane/ControlPlane.Application/
COPY src/ControlPlane/ControlPlane.Infrastructure/ControlPlane.Infrastructure.csproj src/ControlPlane/ControlPlane.Infrastructure/

COPY src/Purchasing/Purchasing.Domain/Purchasing.Domain.csproj                   src/Purchasing/Purchasing.Domain/
COPY src/Purchasing/Purchasing.Application/Purchasing.Application.csproj         src/Purchasing/Purchasing.Application/
COPY src/Purchasing/Purchasing.Infrastructure/Purchasing.Infrastructure.csproj   src/Purchasing/Purchasing.Infrastructure/

COPY src/Accounting/Accounting.Domain/Accounting.Domain.csproj                   src/Accounting/Accounting.Domain/
COPY src/Accounting/Accounting.Application/Accounting.Application.csproj         src/Accounting/Accounting.Application/
COPY src/Accounting/Accounting.Infrastructure/Accounting.Infrastructure.csproj   src/Accounting/Accounting.Infrastructure/

COPY src/Banking/Banking.Domain/Banking.Domain.csproj                   src/Banking/Banking.Domain/
COPY src/Banking/Banking.Application/Banking.Application.csproj         src/Banking/Banking.Application/
COPY src/Banking/Banking.Infrastructure/Banking.Infrastructure.csproj   src/Banking/Banking.Infrastructure/

COPY src/Tax/Tax.Domain/Tax.Domain.csproj                               src/Tax/Tax.Domain/
COPY src/Tax/Tax.Application/Tax.Application.csproj                     src/Tax/Tax.Application/
COPY src/Tax/Tax.Infrastructure/Tax.Infrastructure.csproj               src/Tax/Tax.Infrastructure/

COPY src/Approvals/Approvals.Domain/Approvals.Domain.csproj                   src/Approvals/Approvals.Domain/
COPY src/Approvals/Approvals.Application/Approvals.Application.csproj         src/Approvals/Approvals.Application/
COPY src/Approvals/Approvals.Infrastructure/Approvals.Infrastructure.csproj   src/Approvals/Approvals.Infrastructure/

COPY src/Reporting/Reporting.Application/Reporting.Application.csproj         src/Reporting/Reporting.Application/
COPY src/Reporting/Reporting.Infrastructure/Reporting.Infrastructure.csproj   src/Reporting/Reporting.Infrastructure/

COPY src/HR/HR.Domain/HR.Domain.csproj                               src/HR/HR.Domain/
COPY src/HR/HR.Application/HR.Application.csproj                     src/HR/HR.Application/
COPY src/HR/HR.Infrastructure/HR.Infrastructure.csproj               src/HR/HR.Infrastructure/

COPY src/Invoicing/Invoicing.Domain/Invoicing.Domain.csproj                   src/Invoicing/Invoicing.Domain/
COPY src/Invoicing/Invoicing.Application/Invoicing.Application.csproj         src/Invoicing/Invoicing.Application/
COPY src/Invoicing/Invoicing.Infrastructure/Invoicing.Infrastructure.csproj   src/Invoicing/Invoicing.Infrastructure/

COPY src/FiscalCR/FiscalCR.Domain/FiscalCR.Domain.csproj                     src/FiscalCR/FiscalCR.Domain/
COPY src/FiscalCR/FiscalCR.Application/FiscalCR.Application.csproj           src/FiscalCR/FiscalCR.Application/
COPY src/FiscalCR/FiscalCR.Infrastructure/FiscalCR.Infrastructure.csproj     src/FiscalCR/FiscalCR.Infrastructure/

COPY src/Payroll/Payroll.Domain/Payroll.Domain.csproj                         src/Payroll/Payroll.Domain/
COPY src/Payroll/Payroll.Application/Payroll.Application.csproj               src/Payroll/Payroll.Application/
COPY src/Payroll/Payroll.Infrastructure/Payroll.Infrastructure.csproj         src/Payroll/Payroll.Infrastructure/

COPY src/FiscalMX/FiscalMX.Domain/FiscalMX.Domain.csproj                     src/FiscalMX/FiscalMX.Domain/
COPY src/FiscalMX/FiscalMX.Application/FiscalMX.Application.csproj           src/FiscalMX/FiscalMX.Application/
COPY src/FiscalMX/FiscalMX.Infrastructure/FiscalMX.Infrastructure.csproj     src/FiscalMX/FiscalMX.Infrastructure/

RUN dotnet restore src/Api/Api.WebApi/Api.WebApi.csproj

# ── source code ───────────────────────────────────────────────────────────────
COPY src/Shared/     src/Shared/
COPY src/Api/        src/Api/
COPY src/Parties/    src/Parties/
COPY src/Catalog/    src/Catalog/
COPY src/Sales/      src/Sales/
COPY src/Inventory/  src/Inventory/
COPY src/ControlPlane/ src/ControlPlane/
COPY src/Purchasing/ src/Purchasing/
COPY src/Accounting/ src/Accounting/
COPY src/Banking/    src/Banking/
COPY src/Tax/        src/Tax/
COPY src/Approvals/  src/Approvals/
COPY src/Reporting/  src/Reporting/
COPY src/HR/         src/HR/
COPY src/Invoicing/  src/Invoicing/
COPY src/FiscalCR/   src/FiscalCR/
COPY src/Payroll/    src/Payroll/
COPY src/FiscalMX/   src/FiscalMX/

RUN dotnet publish src/Api/Api.WebApi/Api.WebApi.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=builder --chown=app:app /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:8080/api/health || exit 1
ENTRYPOINT ["dotnet", "Api.WebApi.dll"]
