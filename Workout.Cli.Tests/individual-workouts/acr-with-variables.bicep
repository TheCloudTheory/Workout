var varAcrName = 'myAcr'
var varAcrLocation = 'westeurope'
var varAdminUserEnabled = false

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: varAcrName
  location: varAcrLocation
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: varAdminUserEnabled
  }
}
