param (
	[string] $rg = 'tttech-nervedemo-rg',
	[string] $inputMsgFormat = 'OPC_UA',
	[string] $principalId,
	[string] $dockerserverurl
)

$deployParams = '{ \"principalId\": { \"value\": \"' + $principalId + '\" }, \"inputMsgFormat\": { \"value\": \"' + $inputMsgFormat + '\"}, \"dockerServer\": { \"value\": \"' + $dockerserverurl + '\"} }'

Write-Output ('Deploying Azure resources for the TTTech Nerve Digital twin demo into resource group ' + $rg)
az deployment group create --verbose --resource-group $rg --template-file .\main.bicep --confirm-with-what-if --parameters $deployParams