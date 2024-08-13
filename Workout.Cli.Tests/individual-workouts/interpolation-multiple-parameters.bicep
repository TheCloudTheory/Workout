param parAcrName string
param parIsPrivate bool = false
param parSuffix string

@maxLength(3)
param parEnvironment string = 'dev'

var varAcrName = 'workout${parAcrName}${parEnvironment}${parSuffix}${parIsPrivate ? 'private' : 'public'}'
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
