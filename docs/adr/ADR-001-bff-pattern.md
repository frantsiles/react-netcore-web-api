# ADR-001 — Patrón Backend For Frontend (BFF)

| Campo | Valor |
|-------|-------|
| Estado | Aceptado |
| Fecha | 2025-04 |
| Ámbito | Arquitectura de comunicación cliente-servidor |

---

## Contexto

El frontend React necesita hablar con el Backend API. Las opciones son: llamadas directas desde el navegador al API, un API Gateway con políticas CORS, o un BFF (Backend For Frontend) como intermediario dedicado.

El API emite JWTs — si el frontend habla directamente con él, el token viaja en localStorage o en cabeceras Authorization desde el navegador. Cualquier script de terceros en la página puede robar ese token (XSS). Además, el API está diseñado para ser consumido por múltiples clientes potenciales (apps móviles, herramientas internas), por lo que su contrato de respuesta es genérico.

## Decisión

Introducir un servicio BFF (.NET, `src/BFF/`) que actúa como el único interlocutor del frontend. El frontend nunca llama directamente al Backend API.

El BFF:
- Valida el JWT recibido del frontend antes de proxear la petición al API.
- Puede adaptar las respuestas del API al formato que el frontend necesita (sin exponer detalles internos).
- Es el único servicio que el Vite proxy (dev) y nginx (prod) conocen.

## Consecuencias

**Positivas:**
- El token JWT nunca sale del servidor en producción — el BFF lo guarda en memoria y lo usa para llamar al API. Superficie de XSS reducida.
- El API puede evolucionar (cambiar nombres de campos, reorganizar recursos) sin romper el contrato con el frontend, siempre que el BFF adapte.
- Tests del frontend son más sencillos: solo mockeas el BFF, no el API completo.

**Negativas:**
- Un servicio adicional en el stack = una dependencia más en producción, más imágenes Docker, más pods K8s.
- La lógica de adaptación en el BFF puede convertirse en un "fat BFF" si no se disciplina. En este proyecto el BFF es deliberadamente thin (solo valida y proxea).

## Alternativas descartadas

**API Gateway con CORS:** El Gateway haría las llamadas directas del navegador al API posibles sin BFF, pero requiere configurar CORS con `Access-Control-Allow-Origin` y el token sigue viajando desde el navegador al API. No resuelve el problema de adaptación de contratos.

**Llamadas directas (sin BFF ni Gateway):** Más simple, pero el API queda acoplado al ciclo de cambio del frontend. Cualquier refactor de campo en el API rompe el frontend.
