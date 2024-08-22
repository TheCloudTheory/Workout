param parAcrName string
param parIsPrivate bool = false

var varAcrName = 'workout${parAcrName}'
var varAcrLocation = 'westeurope'
var varAdminUserEnabled = parIsPrivate ? false : true

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

output acrName string = acr.name
output acrIsAdminUserEnabled bool = acr.properties.adminUserEnabled
