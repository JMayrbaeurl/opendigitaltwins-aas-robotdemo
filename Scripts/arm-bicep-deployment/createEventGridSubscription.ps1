param (
	[string] $rg = 'tttech-nervedemo-rg'
)

$iothubresourceid=$(az iot hub list -g $rg --query '[0].id' | ConvertFrom-Json)
$adtmapperfuncresourceid=$(az functionapp list -g $rg --query '[0].id' | ConvertFrom-Json) + '/functions/ProcessHubToDTEvents'
$eventSubname='eventSubscription-'+$rg+'-adtmapper'

az eventgrid event-subscription create --name $eventSubname --event-delivery-schema eventgridschema --source-resource-id $iothubresourceid `
--included-event-types Microsoft.Devices.DeviceTelemetry --endpoint-type azurefunction --endpoint $adtmapperfuncresourceid
