@description('Name of the Key Vault (3-24 alphanumeric + hyphens)')
param name string
param location string
param tags object

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true   // use Azure RBAC instead of access policies
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForTemplateDeployment: true
  }
}

output name string = vault.name
output id string = vault.id
output uri string = vault.properties.vaultUri
