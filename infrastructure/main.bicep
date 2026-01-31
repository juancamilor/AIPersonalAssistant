@description('The name of the web app')
param appName string

@description('The location for all resources')
param location string = resourceGroup().location

@description('The SKU for the App Service Plan')
param appServicePlanSku string = 'B1'

@description('The .NET version to use')
param dotnetVersion string = '10.0'

@description('Environment name')
param environment string = 'production'

var appServicePlanName = '${appName}-plan'
var webAppName = appName
var storageAccountName = take(replace(toLower('${appName}store'), '-', ''), 24)

// Storage Account for Travel Map pins
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
  tags: {
    environment: environment
    application: 'ai-personal-assistant'
  }
}

// Blob service for the storage account
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// Container for travel pins
resource travelPinsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'travel-pins'
  properties: {
    publicAccess: 'None'
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
  tags: {
    environment: environment
    application: 'ai-personal-assistant'
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|${dotnetVersion}'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      alwaysOn: true
      http20Enabled: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'AzureStorage__ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'AzureStorage__ContainerName'
          value: 'travel-pins'
        }
      ]
    }
  }
  tags: {
    environment: environment
    application: 'ai-personal-assistant'
  }
}

// Output the web app URL
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output webAppName string = webApp.name
output storageAccountName string = storageAccount.name
