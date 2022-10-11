param (
	[string] $rg = 'tttech-nervedemo-rg',
	[string] $deviceId = 'NerveGateway',
	[string] $dockerserverurl
)

$principalId = az ad signed-in-user show --query 'id' | ConvertFrom-Json

.\deployAzureServices -rg $rg -principalId $principalId -inputMsgFormat 'NerveGateway' -dockerserverurl $dockerserverurl

Write-Output 'Now setting up the Digital Twin instance'
$dtName=$(az dt list -g $rg --query '[0].{Name:name}' | ConvertFrom-Json)
.\createADTGraphForNerveGateway.ps1 -rg $rg -dtName $dtName.Name

Write-Output 'Now registering IoT Hub device'
.\registerIoTHubDevice.ps1 -rg $rg -deviceId $deviceId