param (
	[string] $rg = 'tttech-nervedemo-rg',
    [string] $deviceId = 'kukarobot'
)

$iothub = $(az iot hub list -g $rg --query '[0].{Name:name}' | ConvertFrom-Json)

az iot hub device-identity create --device-id $deviceId -n $iothub.Name -g $rg

return $(az iot hub device-identity connection-string show --device-id $deviceId -n $iothub.Name -g $rg)