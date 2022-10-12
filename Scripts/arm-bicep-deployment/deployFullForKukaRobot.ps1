param (
	[string] $rg = 'tttech-nervedemo-rg',
	[string] $deviceId = 'kukarobot',
	[string] $dockerserverurl
)

$principalId = az ad signed-in-user show --query 'id' | ConvertFrom-Json

.\deployAzureServices -rg $rg -principalId $principalId  -dockerserverurl $dockerserverurl

Write-Output 'Now setting up the Digital Twin instance'
$dtName=$(az dt list -g $rg --query '[0].{Name:name}' | ConvertFrom-Json)
.\createADTGraphForKukaRobot.ps1 -rg $rg -dtName $dtName.Name

Write-Output 'Activating Event subscription for ADT Updates'
.\createEventGridSubscription.ps1 -rg $rg

Write-Output 'Now registering IoT Hub device'
.\registerIoTHubDevice.ps1 -rg $rg -deviceId $deviceId