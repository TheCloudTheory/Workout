# Workout
Test framework for Azure Bicep with dedicated DSL. 

## Current status
As of today, Workout is an experimental project with ongoing development. It's not intended for production use and can introduce breaking changes anytime.

## Approach
Workout is both a test framework and dedicated DSL for writing test cases. Its goal is to give you similar DevEx as you have when writing Bicep files, yet still focus on things important when building test suites. Workout is tightly coupled with capabilities granted by Bicep, but still introduces a couple of additional concepts of its own.

## Quickstart
Run the following command to start workout:
```
./Workout.Cli start workout
```

## Syntax
Syntax for Workout is based on syntax for Bicep, but is development and interpreted as a separate language:
```workout
import './acr.bicep'
import './kv.bicep'

@smoke
test testCase1 = {
    equals(acr.name, 'myacr')
    equals(acr.location, 'eastus')
    equals(acr.sku.name, 'Basic')
    equals(acr.properties.adminUserEnabled, false)
    equals(kv.name, 'mykv')
    equals(kv.properties.tenantId, '00000000-0000-0000-0000-000000000000')
    equals(kv.properties.accessPolicies.0.tenantId, '00000000-0000-0000-0000-000000000000')
}
```
Below you can find detailed description of all keywords available for the Workout language.

### `import`
To import a Bicep file into Workout file, you need to import it. It can be done with `import` keyword:
```workout
import '<path-to-bicep-file>.bicep`
```
The path for the imported file must be relative to the working directory (which is the directory of the Workout file being processed).

### `@smoke`
Optional, marks a test as a smoke test (not yet supported).

### `test`
Each test in a Workout file must start with a `test` keyword. The basic syntax for each test looks like this:
```workout
test <test-name> = {
  <assertions>
}
```
Assertions are a set of logical expressions providing you the ability to write conditions for Workout to validate. Available assertions and their syntax is described below. Note, that assertion must be part of the `test` block, otherwise compilation error is returned by Workout.

### Assertions
Assertions are the most important part of each test as they allow you to access properties of resources defined within Bicep files and write validation logic for them. They have the most complex syntax in Workout hence, we need a dedicated section for them.

#### Accessing properties of a resource
Let's assume the following Bicep (`acr.bicep`) file:
```bicep
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
```
To test it, we could prepare the following Workout file:
```workout
import './acr.bicep'

@smoke
test testCase1 = {
    equals(acr.name, 'myacr')
    equals(acr.location, 'eastus')
    equals(acr.sku.name, 'Basic')
    equals(acr.properties.adminUserEnabled, false)
}
```
To understand the syntax of assertions, you need to understand the connection between Bicep and Workout files. When you import a file (in our example - `acr.bicep`) Workout parses it to build a map linking identifiers of those resources with their definition. In our example, the identifier for the ACR definition is `acr`. As it became available for the Workout file thanks to the `import` keyword, we can access the properties of that resource by referencing them using the identifier:
```
acr.name
acr.location
acr.sku.name
acr.properties.adminUserEnabled
```
When you start Workout using CLI, those references are compiled to reflect the underlying value based on the resource definition. The test case `testCase1` presented above will succeed as all references will have the same values as the values provided in the definition.

#### Accessing array elements
Some resources defined in Bicep will have properties defined as arrays. Workout supports accessing individual elements using syntax similar to most programming languages. Let's consider the following Azure Key Vault definition in Bicep:
```bicep
resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'mykv'
  location: 'eastus'
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: '00000000-0000-0000-0000-000000000000'
    accessPolicies: [
      {
        tenantId: '00000000-0000-0000-0000-000000000000'
        objectId: '00000000-0000-0000-0000-000000000000'
        permissions: {
          keys: []
          secrets: []
          certificates: []
        }
      }
    ]
  }
}
```
To test if the `tenantId` of the first `accessPolicies` array is equal to the tested value, you can use the following syntax:
```workout
@smoke
test testCase1 = {
    equals(kv.properties.accessPolicies.0.tenantId, '00000000-0000-0000-0000-000000000000')
}
```
