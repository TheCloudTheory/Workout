# Workout
Test framework for Azure Bicep

## Assumptions
* The project uses the same DSL as Azure Bicep
* The project allows to run both smoke and E2E tests
* The project must fully support Bicep files

## Quickstart
Run the following command to start workout:
```
./Workout.Cli start workout
```

## Syntax
Let's assume the following Bicep definition:
```bicep
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'myacr'
  location: 'eastus'
  sku: {
    name: 'Basic'
  }
}

output outputAcrId string = acr.id
```
A test for that definition could look like this.

### V1
```
import './acr.bicep'

@smoke
test smokeTest = {
    assert(acr.name == 'acr)
    assert(acr.location = 'eastus')
    assert(acr.sku.name == 'Basic)
}

@e2e
test e2eTest = {
    assert(contains(outputAcrId, 'acr'))
}
```

### V2
```
import './acr.bicep'

@smoke
test smokeTest = {
    equals(acr.name, 'acr)
    equals(acr.location, 'eastus')
    equals(acr.sku.name, 'Basic)
}

@e2e
test e2eTest = {
    contains(outputAcrId, 'acr')
}
```
