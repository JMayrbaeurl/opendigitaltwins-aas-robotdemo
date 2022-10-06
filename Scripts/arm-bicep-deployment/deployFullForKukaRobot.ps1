param (
	[string] $rg = 'tttech-nervedemo-rg'
)

$principalId = az ad signed-in-user show --query 'userPrincipalName' | ConvertFrom-Json

.\deployAzureServices -rg $rg -principalId $principalId

Write-Output 'Now setting up the Digital Twin instance'
$dtName=$(az dt list -g tttech-nervedemo-rg --query '[0].{Name:name}' | ConvertFrom-Json)
.\createADTGraphForKukaRobot.ps1 -rg $rg -dtName $dtName.Name

Write-Output 'Now registering IoT Hub device'
.\registerIoTHubDevice.ps1 -rg $rg