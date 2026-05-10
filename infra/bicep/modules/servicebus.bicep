@description('Name of the Service Bus namespace')
param name string
param location string
param tags object

resource namespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

// Topic for user domain events
resource userEventsTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: namespace
  name: 'user-events'
  properties: {
    defaultMessageTimeToLive: 'P14D'
    enablePartitioning: false
  }
}

resource userCreatedSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: userEventsTopic
  name: 'user-created-subscription'
  properties: {
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 3
  }
}

resource userDeletedSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: userEventsTopic
  name: 'user-deleted-subscription'
  properties: {
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 3
  }
}

output namespaceName string = namespace.name
output id string = namespace.id
output serviceBusEndpoint string = namespace.properties.serviceBusEndpoint
