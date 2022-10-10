param (
	[string] $rg = 'tttech-work-rg',
    [string] $imagename = 'nervedemofuncs:latest'
)

Write-Output ('Deploying Azure resources to build artefacts for the TTTech Nerve Digital twin demo ' + $rg)
az deployment group create --verbose --resource-group $rg --template-file .\build.bicep --confirm-with-what-if

$regName=$(az acr list -g $rg --query '[0].{Name:name}' | ConvertFrom-Json)
az acr build -f ..\..\adtinoutfunctions\Dockerfile --registry $regName.Name --resource-group $rg --image $imagename ..\..\adtinoutfunctions\

Write-Output ('Image can be pulled from: ' + $regName + "azurecr.io" + $imagename)