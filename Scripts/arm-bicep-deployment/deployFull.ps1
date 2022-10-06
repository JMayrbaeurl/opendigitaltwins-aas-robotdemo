param (
	[string] $rg = 'tttech-nervedemo-rg'
)

.\deployAzureServices -rg $rg

Write-Output 'Now setting up the Digital Twin instance'
$dtName=$(az dt list -g tttech-nervedemo-rg --query '[0].{Name:name}' | ConvertFrom-Json)
.\createADTGraph.ps1 -rg $rg -dtName $dtName.Name

Write-Output 'Now registering IoT Hub device'
.\registerIoTHubDevice.ps1 -rg $rg