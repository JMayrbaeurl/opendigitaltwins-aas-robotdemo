@description('Location of all resources')
param location string = resourceGroup().location

@description('Name given to Digital Twins resource')
param digitalTwinsName string = 'digitalTwins-${uniqueString(resourceGroup().id)}'

// Tag for created Azure resources
param tag_WorkloadName string = 'Digital Twins Robot demo'
param tag_DataClassification string = 'General'
param tag_Criticality string = 'Medium'
param tag_ApplicationName string = 'TTTech Nerve ADT'
param tag_Env string = 'Test'

// Create IoT Hub
var iotHubName = 'iothub${uniqueString(resourceGroup().id)}'
resource IoTHub 'Microsoft.Devices/IotHubs@2021-07-02' = {
  name: iotHubName
  location: location
  sku: {
    name: 'S1'
    capacity: 1
  }
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
}
output IoTHub string = iotHubName

// Create Event Grid system topic for IoT Hub messages
resource ADTSystemTopic 'Microsoft.EventGrid/systemTopics@2022-06-15' = {
  name: 'adt-system-topic-${uniqueString(resourceGroup().id)}'
  location: location
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
  properties: {
    source: IoTHub.id
    topicType: 'Microsoft.Devices.IoTHubs'
  }
}

// Create Function app and subscribe to Iot Hub device messages topic
param applicationName string = 'tttech-apps-${uniqueString(resourceGroup().id)}'

@description('Function app name')
@minLength(2)
param webAppName string = 'adt-mapper-${uniqueString(resourceGroup().id)}'

@description('The Runtime stack of current web app')
param linuxFxVersion string = 'DOCKER|tttechdemo.azurecr.io/nervedemo-adt-mapper:latest'

var storageAccountName = '${uniqueString(resourceGroup().id)}functions'

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: storageAccountName
  location: location
  kind: 'Storage'
  sku: {
    name: 'Standard_LRS'
  }
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: applicationName
  location: location
  kind: 'linux'
  properties: {
    reserved: true
  }
  sku: {
    name: 'B1'
    capacity: 1
  }
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
}

resource webApp 'Microsoft.Web/sites@2021-02-01' = {
  name: webAppName
  location: location
  kind: 'functionapp'
  properties: {
    httpsOnly: true
    serverFarmId: hostingPlan.id
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      minTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
}

/*
resource funcProcessHubToDTEvents 'Microsoft.Web/sites/functions@2022-03-01' = {
  name: 'ProcessHubToDTEvents'
  parent: webApp
}
*/

resource appsettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: webApp
  name: 'appsettings'
  properties: {
    AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, '2021-09-01').keys[0].value}'
    FUNCTIONS_EXTENSION_VERSION: '~3'
    ADT_SERVICE_URL: 'https://${digitalTwins.properties.hostName}'
    ftpsState: 'Disabled'
    minTlsVersion: '1.2'
  }
}

// Azure RBAC Guid Source: https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
var azureRbacContributor = 'b24988ac-6180-42a0-ab88-20f7382dd24c'
var azureRbacAzureEventHubsDataOwner = 'f526a384-b230-433a-b45c-95f59c4a2dec'
var azureRbacAzureDigitalTwinsDataOwner = 'bcd981a7-7f74-457b-83e1-cceb9e632ffe'

// Assigns the given principal id input data owner of Digital Twins resource
resource givenIdToDigitalTwinsRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  name: guid(digitalTwins.id, 'functionToADT', azureRbacAzureDigitalTwinsDataOwner)
  scope: digitalTwins
  properties: {
    principalId: webApp.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureRbacAzureDigitalTwinsDataOwner)
    principalType: 'ServicePrincipal'
  }
}
/*
resource eventSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2021-12-01' = {
  parent: ADTSystemTopic
  name: 'eventSubscription-${uniqueString(resourceGroup().id)}'
  properties: {
    destination: {
      properties: {
        resourceId: '${webApp.id}/${funcProcessHubToDTEvents.name}'
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
      endpointType: 'AzureFunction'
    }
    filter: {
      includedEventTypes: [
        'Microsoft.Devices.DeviceTelemetry'
      ]
      enableAdvancedFilteringOnArrays: true
    }
  }
}
*/
// Creates Digital Twins instance
resource digitalTwins 'Microsoft.DigitalTwins/digitalTwinsInstances@2022-05-31' = {
  name: digitalTwinsName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
}

@description('Name given to Azure Data Explorer cluster resource')
param adxClusterName string = 'adx${uniqueString(resourceGroup().id)}'

@description('Name given to database')
param databaseName string = 'database-${uniqueString(resourceGroup().id)}'

// Creates Azure Data Explorer cluster
resource adxCluster 'Microsoft.Kusto/Clusters@2022-02-01' = {
  name: adxClusterName
  location: location
  sku: {
    capacity: 1
    name: 'Dev(No SLA)_Standard_D11_v2'
    tier: 'Basic'
  }
  identity: {
    type: 'None'
  }
  properties: {
    enableAutoStop: false
  }
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
}

// Creates database under the Azure Data Explorer cluster
resource database 'Microsoft.Kusto/clusters/databases@2022-02-01' = {
  name: '${adxCluster.name}/${databaseName}'
  location: location
  kind: 'ReadWrite'
  properties: {
    hotCachePeriod: 'P30D'
    softDeletePeriod: 'P1Y'
  }
}

@description('Name given to Event Hubs namespace resource')
param eventHubsNamespaceName string = 'ehns-${uniqueString(resourceGroup().id)}'

@description('Name given to event hub resource')
param eventHubName string = 'ADTDataHistory'

@allowed([
  'Basic'
  'Standard'
])
@description('Event Hubs namespace SKU billing tier')
param eventHubsNamespaceTier string = 'Basic'

@description('Event Hubs throughput units')
param eventHubsNamespaceCapacity int = 1

@allowed([
  'Basic'
  'Premium'
  'Standard'
])
@description('Event Hubs namespace SKU option')
param eventHubsNamespacePlan string = 'Basic'

@description('Number of days to retain data in event hub')
param retentionInDays int = 1

@description('Number of partitions to create in event hub')
param partitionCount int = 2

// Creates Event Hubs namespace
resource eventHubsNamespace 'Microsoft.EventHub/namespaces@2021-11-01' = {
  name: eventHubsNamespaceName
  location: location
  sku: {
    capacity: eventHubsNamespaceCapacity
    name: eventHubsNamespacePlan
    tier: eventHubsNamespaceTier
  }
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
}

// Creates an event hub in the Event Hubs namespace
resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  parent: eventHubsNamespace
  name: eventHubName
  properties: {
    messageRetentionInDays: retentionInDays
    partitionCount: partitionCount
  }
}

// Assigns Digital Twins resource data owner of event hub
resource digitalTwinsToEventHubRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  name: guid(eventHub.id, 'dtToEventHub', azureRbacAzureEventHubsDataOwner)
  scope: eventHub
  properties: {
    principalId: digitalTwins.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureRbacAzureEventHubsDataOwner)
    principalType: 'ServicePrincipal'
  }
}

// Assigns Digital Twins resource admin assignment to database
resource digitalTwinsToDatabasePrincipalAssignment 'Microsoft.Kusto/clusters/databases/principalAssignments@2022-02-01' = {
  name: '${adxClusterName}/${databaseName}/${guid(database.id, 'dbAdmin', 'Admin')}'
  properties: {
    principalId: digitalTwins.identity.principalId
    role: 'Admin'
    tenantId: digitalTwins.identity.tenantId
    principalType: 'App'
  }
}

// Assigns Digital Twins resource contributor assignment to database
resource digitalTwinsToDatabaseRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  name: guid(database.id, 'dbAdmin', azureRbacContributor)
  scope: database
  properties: {
    principalId: digitalTwins.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureRbacContributor)
    principalType: 'ServicePrincipal'
  }
}

var eventHubEndpoint = 'sb://${eventHubsNamespaceName}.servicebus.windows.net'
@description('Name given to table in database')
param databaseTableName string = 'databaseTable-${uniqueString(resourceGroup().id)}'

// Creates a time series database connection between the Digital Twin resource and Azure Data Explorer cluster table
resource tsdbConnection 'Microsoft.DigitalTwins/digitalTwinsInstances/timeSeriesDatabaseConnections@2022-05-31' = {
  name: '${digitalTwinsName}/${databaseTableName}'
  properties: {
    connectionType: 'AzureDataExplorer'
    adxEndpointUri: adxCluster.properties.uri
    adxDatabaseName: databaseName
    eventHubEndpointUri: eventHubEndpoint
    eventHubEntityPath: eventHubName
    adxResourceId: adxCluster.id
    eventHubNamespaceResourceId: eventHubsNamespace.id
  }
}

@description('Name given to Grafana dashboard resource')
param grafanaDashboardName string = 'dashboard-${uniqueString(resourceGroup().id)}'

// Create Grafana dashboard
resource dashboard 'Microsoft.Dashboard/grafana@2022-08-01' = {
  name: grafanaDashboardName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Standard'
  }
  properties: {
  }
  tags: {
    WorkloadName: tag_WorkloadName
    DataClassification: tag_DataClassification
    Criticality: tag_Criticality
    ApplicationName: tag_ApplicationName
    Env: tag_Env
  }
}
