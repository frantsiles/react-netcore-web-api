#!/usr/bin/env bash
# Deploy full infrastructure to Azure and apply Kubernetes manifests.
# Usage: ./infra/deploy.sh [dev|staging|prod]
set -euo pipefail

ENV="${1:-dev}"
PROJECT="demok8s"
RESOURCE_GROUP="rg-${PROJECT}-${ENV}"
LOCATION="eastus"

echo "=================================================="
echo "  Deploying to: ${ENV} (${RESOURCE_GROUP})"
echo "=================================================="
read -p "Continue? (y/N) " confirm
[[ "${confirm}" =~ ^[Yy]$ ]] || { echo "Aborted."; exit 1; }

# 1. Login
echo ">> Logging in to Azure..."
az login --use-device-code

# 2. Resource group
echo ">> Creating resource group ${RESOURCE_GROUP}..."
az group create --name "${RESOURCE_GROUP}" --location "${LOCATION}" --output none

# 3. Deploy Bicep infrastructure
echo ">> Deploying Bicep templates..."
DEPLOYMENT=$(az deployment group create \
    --resource-group "${RESOURCE_GROUP}" \
    --template-file "infra/bicep/main.bicep" \
    --parameters environment="${ENV}" projectName="${PROJECT}" \
    --output json)

ACR_SERVER=$(echo "${DEPLOYMENT}" | jq -r '.properties.outputs.acrLoginServer.value')
AKS_NAME=$(echo "${DEPLOYMENT}"   | jq -r '.properties.outputs.aksName.value')

echo "   ACR:  ${ACR_SERVER}"
echo "   AKS:  ${AKS_NAME}"

# 4. AKS credentials
echo ">> Getting AKS credentials..."
az aks get-credentials \
    --resource-group "${RESOURCE_GROUP}" \
    --name "${AKS_NAME}" \
    --overwrite-existing

# 5. Build & push images
echo ">> Logging in to ACR ${ACR_SERVER}..."
az acr login --name "${ACR_SERVER%%.*}"

TAG="1.0.0"
for SERVICE in api bff gateway worker frontend; do
    echo ">> Building and pushing demo/${SERVICE}:${TAG}..."
    docker build -t "${ACR_SERVER}/demo/${SERVICE}:${TAG}" \
        -f "docker/${SERVICE}/Dockerfile" .
    docker push "${ACR_SERVER}/demo/${SERVICE}:${TAG}"
done

# 6. Update overlay and apply K8s manifests
echo ">> Updating ACR name in K8s overlay..."
sed -i "s|<ACR_NAME>|${ACR_SERVER%%.*}|g" k8s/overlays/azure/kustomization.yaml

echo ">> Applying Kubernetes manifests (overlay: azure)..."
kubectl apply -k k8s/overlays/azure

echo ""
echo "=================================================="
echo "  Deployment complete!"
echo "  Ingress:  kubectl get ingress -n demo"
echo "  Pods:     kubectl get pods -n demo"
echo "  Grafana:  kubectl port-forward -n demo-monitoring svc/grafana 3001:3000"
echo "=================================================="
