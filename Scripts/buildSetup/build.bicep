@description('Location of all resources')
param location string = resourceGroup().location

@description('Name given to Container registry')
param registryName string = 'images${uniqueString(resourceGroup().id)}'

// Tag for created Azure resources
param tag_WorkloadName string = 'Digital Twins Robot demo'
param tag_DataClassification string = 'General'
param tag_Criticality string = 'Medium'
param tag_ApplicationName string = 'TTTech Nerve ADT'
param tag_Env string = 'Dev'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2022-02-01-preview' = {
  name: registryName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    anonymousPullEnabled: true
  }
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
}
