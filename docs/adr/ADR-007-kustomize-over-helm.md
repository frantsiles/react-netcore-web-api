# ADR-007 — Kustomize en lugar de Helm para los manifiestos Kubernetes

| Campo | Valor |
|-------|-------|
| Estado | Aceptado |
| Fecha | 2025-05 |
| Ámbito | Gestión de manifiestos Kubernetes |

---

## Contexto

Los manifiestos Kubernetes necesitan variar entre entornos: en local (minikube/kind) las imágenes tienen `imagePullPolicy: Never` y se cargan localmente; en Azure las imágenes vienen de ACR con el registry completo. Hay dos herramientas principales para gestionar esta variabilidad: Helm y Kustomize.

## Decisión

Usar **Kustomize** con una estructura base + overlays:

```
k8s/
├── base/          ← manifiestos comunes a todos los entornos
└── overlays/
    ├── local/     ← patch: imagePullPolicy Never
    └── azure/     ← patch: imágenes de ACR
```

`kubectl apply -k k8s/overlays/local` aplica la base + los patches del overlay sin necesidad de herramientas adicionales — Kustomize está integrado en `kubectl` desde v1.14.

## Consecuencias

**Positivas:**
- **Zero dependencias**: Kustomize viene con `kubectl`. No hay que instalar `helm` ni gestionar chart repositories.
- **Legibilidad**: los manifiestos base son YAML puro de Kubernetes, no templates con `{{ .Values.something }}`. Cualquiera que sepa K8s puede leerlos directamente.
- **Patches quirúrgicos**: el overlay solo sobreescribe lo que cambia. No hay que duplicar toda la definición de un Deployment para cambiar una imagen.
- **Sin estado**: Kustomize no tiene concept de "release" ni estado en el cluster. Lo que ves en el repo es exactamente lo que hay en el cluster.

**Negativas:**
- Kustomize tiene menos expresividad que Helm para configuración dinámica compleja (condicionales, bucles, funciones de transformación).
- Si el proyecto necesitara distribuirse como un chart reutilizable por terceros, Helm sería la elección natural.
- Los patches de Kustomize (Strategic Merge Patch, JSON Patch) tienen su propia curva de aprendizaje.

## Alternativas descartadas

**Helm:** Excelente cuando el chart se va a reutilizar en múltiples proyectos o distribuir en un repositorio de charts. En este proyecto, los manifiestos son específicos de esta aplicación — el valor de Helm (templating reutilizable) no aplica. La complejidad de `values.yaml` + templates tendría más costo que beneficio.

**Manifiestos planos por entorno:** Duplicar `deployment-local.yaml` y `deployment-azure.yaml` es el approach más simple, pero mantener los dos sincronizados cuando cambia algo en la base es propenso a errores.

**Skaffold:** Interesante para inner loop de desarrollo (hot reload en K8s), pero es una herramienta de developer workflow, no de gestión de manifiestos de producción.
