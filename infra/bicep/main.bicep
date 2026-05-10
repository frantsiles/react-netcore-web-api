@description('Base name for all resources')
param projectName string = 'demok8s'

@description('Azure region')
param location string = resourceGroup().location

@description('Environment: dev, staging, prod')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Kubernetes version for AKS')
param kubernetesVersion string = '1.30.0'

var tags = {
  project: projectName
  environment: environment
  managedBy: 'bicep'
  repository: 'react-netcore-web-api'
}

module acr 'modules/acr.bicep' = {
  name: 'acr-deployment'
  params: {
    name: '${projectName}${environment}acr'
    location: location
    tags: tags
  }
}

module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault-deployment'
  params: {
    name: '${projectName}-${environment}-kv'
    location: location
    tags: tags
  }
}

module servicebus 'modules/servicebus.bicep' = {
  name: 'servicebus-deployment'
  params: {
    name: '${projectName}-${environment}-sb'
    location: location
    tags: tags
  }
}

module aks 'modules/aks.bicep' = {
  name: 'aks-deployment'
  params: {
    name: '${projectName}-${environment}-aks'
    location: location
    tags: tags
    kubernetesVersion: kubernetesVersion
    acrId: acr.outputs.id
  }
}

output aksName string = aks.outputs.name
output acrLoginServer string = acr.outputs.loginServer
output serviceBusNamespace string = servicebus.outputs.namespaceName
output keyVaultName string = keyvault.outputs.name
output aksKubeconfig string = aks.outputs.name
