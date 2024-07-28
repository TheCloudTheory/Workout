resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'myacr'
  location: 'eastus'
  sku: {
    name: 'Basic'
  }
}

output outputAcrId string = acr.id
