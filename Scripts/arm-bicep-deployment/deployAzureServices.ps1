param (
	[string] $rg = 'tttech-nervedemo-rg',
	[string] $principalId
)

$deployParams = '{ \"principalId\": { \"value\": \"' + $principalId + '\" } }'

Write-Output ('Deploying Azure resources for the TTTech Nerve Digital twin demo into resource group ' + $rg)
az deployment group create --verbose --resource-group $rg --template-file .\main.bicep --confirm-with-what-if --parameters $deployParams