@description('Location for all resources.')
param location string = resourceGroup().location

@description('The location of the data for the Communication Services resource.')
param acsDataLocation string = 'Australia'

@description('A prefix to add to the start of all resource names. Note: A "unique" suffix will also be added')
param prefix string = 'demo'

@description('The principal ID to assign the roles to (developer), if left empty or set to "none" no role assignments will be created. This is the object id of the user or service principal.')
param principalId string
param principalType ('User' | 'ServicePrincipal') = 'User'
param tags object = {}

var uniqueNameFormat = '${prefix}-{0}-${uniqueString(resourceGroup().id, prefix)}'
var uniqueShortNameFormat = toLower('${prefix}{0}${uniqueString(resourceGroup().id, prefix)}')
var usePrincipalId = !empty(trim(principalId)) && toLower(trim(principalId)) != 'none'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: format(uniqueNameFormat, 'logs')
  location: location
  tags: tags
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource acs 'Microsoft.Communication/communicationServices@2023-06-01-preview' = {
  name: format(uniqueNameFormat, 'acs')
  location: 'global'
  tags: tags
  properties: {
    dataLocation: acsDataLocation
  }
}

resource acsLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'allLogsToLogAnalytics'
  scope: acs
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
  }
}

resource acsOwnerRole 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: subscription()
  // Communication and Email Service Owner
  name: '09976791-48a7-449e-bb21-39d1a415f350'
}

resource acsOwnerRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (usePrincipalId) {
  name: guid(acs.id, acsOwnerRole.id, principalId)
  scope: acs
  properties: {
    roleDefinitionId: acsOwnerRole.id
    principalId: principalId
    principalType: principalType
  }
}

resource aiSpeech 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: format(uniqueNameFormat, 'aispeech')
  location: location
  tags: tags
  kind: 'SpeechServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: format(uniqueShortNameFormat, 'aispeech')
    disableLocalAuth: usePrincipalId
  }
}

resource aiSpeechLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'allLogsToLogAnalytics'
  scope: aiSpeech
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
  }
}

resource aiSpeechUserRole 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: subscription()
  // Cognitive Services Speech User
  name: 'f2dc8367-1007-4938-bd23-fe263f013447'
}

resource aiSpeechRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (usePrincipalId) {
  name: guid(aiSpeech.id, aiSpeechUserRole.id, principalId)
  scope: aiSpeech
  properties: {
    roleDefinitionId: aiSpeechUserRole.id
    principalId: principalId
    principalType: principalType
  }
}

resource aoai 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: format(uniqueNameFormat, 'aoai')
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: format(uniqueNameFormat, 'aoai')
    disableLocalAuth: usePrincipalId
  }
  resource gpt4o 'deployments@2023-10-01-preview' = {
    name: 'gpt-4o'
    sku: {
      name: 'GlobalStandard'
      capacity: 50
    }
    properties: {
      model: {
        format: 'OpenAI'
        name: 'gpt-4o'
        version: '2024-08-06'
      }
      versionUpgradeOption: 'NoAutoUpgrade'
    }
  }
}

resource aoaiLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'allLogsToLogAnalytics'
  scope: aoai
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
  }
}

resource aoaiUserRole 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  name: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd' // Cognitive Services OpenAI User
  scope: subscription()
}

resource aoaiUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (usePrincipalId) {
  name: guid(aoaiUserRole.id, aiSpeechUserRole.id, principalId)
  scope: aoai
  properties: {
    roleDefinitionId: aoaiUserRole.id
    principalId: principalId
    principalType: principalType
  }
}

output ACSEndpoint string = acs.properties.hostName
output AISpeechResourceID string = aiSpeech.id
output AISpeechRegion string = aiSpeech.location
output AOAIEndpoint string = aoai.properties.endpoint
output AOAIModelDeployment string = aoai::gpt4o.name
