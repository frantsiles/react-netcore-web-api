# Skill: Infrastructure Manager

Eres un experto en infraestructura cloud-native para este repositorio. Cuando el usuario invoque este skill, analiza el estado actual del proyecto y ayúdale con la gestión de contenedores, Kubernetes y Azure.

## Contexto del proyecto

- **Arquitectura**: React (SPA) → YARP Gateway (:5000) → BFF (:5001) → Backend API (:5002)
- **Servicios adicionales**: Worker Service (MassTransit + RabbitMQ/Service Bus), Azure Functions
- **Observabilidad**: OTel Collector → Prometheus + Loki + Tempo → Grafana
- **Contenedores**: Dockerfiles en `docker/<servicio>/`; contexto siempre desde la raíz del repo
- **Orquestación**: `docker-compose.yml` (stack completo), `docker-compose.infra.yml` (solo infra)
- **Kubernetes**: Kustomize en `k8s/` — base + overlays (`local` para minikube/kind, `azure` para AKS)
- **IaC Azure**: Bicep en `infra/bicep/` — módulos AKS, ACR, Service Bus, Key Vault
- **Deploy AKS**: `./infra/deploy.sh [dev|staging|prod]`

## Comandos frecuentes

### Docker local
```bash
# Construir todas las imágenes
docker compose build

# Levantar stack completo + observabilidad
docker compose --profile observability up -d

# Solo infraestructura (desarrollando servicios localmente)
docker compose -f docker-compose.infra.yml up -d

# Ver logs de un servicio
docker compose logs -f api

# Reconstruir un servicio
docker compose up -d --build api
```

### Kubernetes local (minikube)
```bash
# Cargar imágenes en minikube
minikube image load demo/api:latest
minikube image load demo/bff:latest
minikube image load demo/gateway:latest
minikube image load demo/worker:latest
minikube image load demo/frontend:latest

# Aplicar manifiestos locales
kubectl apply -k k8s/overlays/local

# Ver estado
kubectl get all -n demo

# Port-forward para acceso local
kubectl port-forward -n demo svc/frontend 8080:80
kubectl port-forward -n demo-monitoring svc/grafana 3001:3000
```

### Kubernetes (kind)
```bash
# Cargar imágenes en kind
kind load docker-image demo/api:latest
kind load docker-image demo/bff:latest
# ... etc.
kubectl apply -k k8s/overlays/local
```

### Azure AKS
```bash
# Deploy completo (Bicep + build + push + kubectl apply)
./infra/deploy.sh dev

# Solo aplicar manifiestos (si las imágenes ya están en ACR)
kubectl apply -k k8s/overlays/azure

# Ver ingress
kubectl get ingress -n demo
```

## Cuando el usuario pida ayuda con infraestructura

1. **Diagnóstico**: Lee los archivos relevantes (`docker-compose.yml`, manifiestos K8s, Dockerfiles) para entender el estado actual.
2. **Problema con Docker**: Verifica health checks, dependencias entre servicios, puertos.
3. **Problema con K8s**: Verifica deployments, services, ingress, secrets y configmaps.
4. **Problema con observabilidad**: Verifica la cadena OTel Collector → Prometheus/Loki/Tempo → Grafana. Chequea el config en `docker/otel-collector/otelcol-config.yaml`.
5. **Añadir nuevo servicio**: Crear Dockerfile, añadir al docker-compose, crear manifiestos K8s en `k8s/base/<servicio>/`, añadir imagen al overlay.
6. **Actualizar Bicep**: Los módulos están en `infra/bicep/modules/`. El punto de entrada es `main.bicep`.

## Puntos críticos de configuración

- El secreto JWT debe ser idéntico en `api` y `bff`. En Docker se pasa como env var `Jwt__Secret`.
- En K8s el secreto está en `k8s/base/api/secret.yaml` — **cambiar antes de producción**.
- El OTel endpoint en local es `http://localhost:4317`; en Docker/K8s es `http://otel-collector:4317`.
- La imagen del frontend incluye nginx que proxea `/bff/` al servicio BFF — ver `docker/frontend/nginx.conf`.
- El Worker soporta dos transportes: `RabbitMQ` (local/Docker) y `AzureServiceBus` (Azure). Controlado por `MessageBus:Transport`.
