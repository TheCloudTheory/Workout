param parAcrName string
param parAcrLocation string = resourceGroup().location
param parAdminUserEnabled bool = false

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: parAcrName
  location: parAcrLocation
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: parAdminUserEnabled
  }
}
