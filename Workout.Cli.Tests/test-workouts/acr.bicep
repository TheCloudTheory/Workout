resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'myacr'
  location: 'eastus'
  sku: {
    name: 'Basic'
  }
}

resource acr2 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'myacr2'
  location: 'westeurope'
  sku: {
    name: 'Standard'
  }
}

output outputAcrId string = acr.id
