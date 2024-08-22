module acr_private 'acr-with-outputs.bicep' = {
  name: 'acr-with-params-and-variables-private'
  params: {
    parAcrName: 'modules1'
    parIsPrivate: true
  }
}

module acr_public 'acr-with-outputs.bicep' = {
  name: 'acr-with-params-and-variables-public'
  params: {
    parAcrName: 'modules2'
    parIsPrivate: false
  }
}

output outAcrPrivateName string = acr_private.outputs.acrName
output outAcrPublicName string = acr_public.outputs.acrName
