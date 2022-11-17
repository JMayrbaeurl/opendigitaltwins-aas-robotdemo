param (
	[string] $rg = 'tttech-nervedemo-rg',
    [string] $deviceId = 'kukarobot'
)

$iothub = $(az iot hub list -g $rg --query '[0].{Name:name}' | ConvertFrom-Json)
$Stoploop = $false
[int]$Retrycount = "0"
 
do {
    $iothub = $(az iot hub list -g $rg --query '[0].{Name:name}' | ConvertFrom-Json)
    az iot hub device-identity create --device-id $deviceId -n $iothub.Name -g $rg --output none

    if ($? -eq $true) {
        $Stoploop = $true
    }
    else {
        if ($Retrycount -gt 3) {
            Write-Host "Could not register device on IoT Hub after 3 retrys. Check your IoT Hub configuration"
            $Stoploop = $true
        }
        else {
            Write-Host "Could not register device on IoT Hub. Retrying in 60 seconds..."
            Start-Sleep -Seconds 60
            $Retrycount = $Retrycount + 1
        }
    }
}
While ($Stoploop -eq $false)

return $(az iot hub device-identity connection-string show --device-id $deviceId -n $iothub.Name -g $rg)