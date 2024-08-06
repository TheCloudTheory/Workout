resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: 'myacr'
  location: 'eastus'
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}
