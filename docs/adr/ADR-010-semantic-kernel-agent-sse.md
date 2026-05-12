# ADR-010: Agente IA con Semantic Kernel + SSE

**Estado**: Aceptado  
**Fecha**: 2026-05-12

## Contexto

El sistema necesita un agente conversacional que responda preguntas en lenguaje natural sobre
usuarios y sesiones, ejecutando acciones reales (consultar, crear, revocar) contra los handlers
MediatR ya existentes, con respuesta en streaming token a token.

## Decisiones

### 1. Semantic Kernel 1.76.0 como orquestador de agente

SK proporciona abstracciones de plugins, tool calling automático y streaming de chat sobre
múltiples providers LLM (OpenAI, Azure OpenAI, Ollama) con una única API.

### 2. Plugins sobre ISender, no sobre HTTP interno

Los plugins (`UserPlugin`, `SessionPlugin`) inyectan `ISender` y llaman directamente a los
handlers MediatR en lugar de hacer peticiones HTTP al propio API.

**Por qué**: evita el overhead de red de localhost-a-localhost, respeta el principio de
responsabilidad única (el agente es lógica de aplicación, no un cliente externo) y reutiliza
la validación y la lógica de dominio sin duplicar código.

**Alternativa descartada**: llamar al API REST interno con `HttpClient`. Añade latencia,
requiere gestión de tokens adicional y viola la arquitectura en capas.

### 3. SSE (Server-Sent Events) para streaming

El endpoint `/api/assistant` emite tokens via SSE (`text/event-stream`) en lugar de esperar
a la respuesta completa.

**Por qué SSE vs WebSocket**: SSE es unidireccional (suficiente para respuestas del agente),
nativo en el browser sin librerías extra, compatible con proxies HTTP estándar y más simple
que WS. SignalR ya existe en el proyecto para comunicación bidireccional (notificaciones de
sesión); no hay razón para reutilizarlo para un canal unidireccional.

### 4. IKernelFactory con múltiples providers

`IKernelFactory` desacopla la selección del provider LLM de `AssistantService`. La
configuración `Assistant:Provider` elige entre `OpenAIKernelFactory`,
`AzureOpenAIKernelFactory` u `OllamaKernelFactory` en DI.

**Por qué**: el proyecto es un showcase que debe demostrar independencia de vendor. Ollama
como default permite desarrollo y demos sin coste ni API key. Cambiar de provider es una
línea de configuración.

**Tradeoff aceptado**: el conector Ollama (`Microsoft.SemanticKernel.Connectors.Ollama`)
está en versión `1.76.0-alpha`. Se acepta porque es el conector oficial de Microsoft y el
fallback a OpenAI/Azure OpenAI es trivial.

### 5. BFF como streaming pass-through

El BFF no deserializa la respuesta SSE del API: copia el stream directamente con
`HttpCompletionOption.ResponseHeadersRead` + `CopyToAsync`.

**Por qué**: SSE es un protocolo de streaming continuo. Deserializar y re-serializar cada
chunk rompería el contrato de tiempo real con el cliente y añadiría latencia innecesaria.
Esta es una excepción documentada al patrón de proxy estándar del BFF; en el resto de
endpoints el BFF sí deserializa y remodela las respuestas.

## Consecuencias

- El agente tiene acceso a todos los handlers disponibles — en el futuro se pueden añadir
  más plugins sin modificar `AssistantService`.
- El tiempo de respuesta depende del LLM configurado; con Ollama local puede ser más lento
  que con OpenAI/Azure en producción.
- Los plugins usan `RequesterIsAdmin: true` en `RevokeSessionCommand` — el endpoint está
  protegido con JWT, por lo que la autorización real la gestiona el middleware de ASP.NET.
