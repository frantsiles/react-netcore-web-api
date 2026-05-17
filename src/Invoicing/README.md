# Invoicing (E10) — Esqueleto

Este módulo implementa el agregado `Invoice` y la lógica básica para convertir `SalesOrder` → `Invoice`.

Estructura inicial:
- Invoicing.Domain: entidades `Invoice`, `InvoiceLine`.
- Invoicing.Application: comando `ConvertSalesOrderToInvoiceCommand` y handler esqueleto.
- Invoicing.WebApi: `InvoicesController` con endpoint POST `/api/invoicing/convert`.

Este es solo un punto de partida (esqueleto). Implementaciones y referencias a otros proyectos (Sales, Accounting, Inventory) deben añadirse cuando se integre.
