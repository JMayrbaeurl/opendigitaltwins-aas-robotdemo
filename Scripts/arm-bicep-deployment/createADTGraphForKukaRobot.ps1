param (
	[string] $dtName,
	[string] $rg = 'tttech-nervedemo-rg'
)

# First make the currently logged in user a 'Azure Digital Twins Data Owner'
$principalId = az ad signed-in-user show --query 'userPrincipalName' | ConvertFrom-Json
az dt role-assignment create -n $dtName --assignee $principalId --role "Azure Digital Twins Data Owner" -g $rg

# Next upload all models from the AAS ontology
az dt model create -n $dtName --from-directory .\Ontology -g $rg

# Then start with the core nodes of the Kuka robot shell
az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:AssetAdministrationShell;1"`
 --twin-id Kuka_KR3_540_Agilus --properties ".\graph\Kukarobot.json" -g $rg

az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:AssetInformation;1"`
 --twin-id Kukarobot_AssetInfo --properties ".\graph\Kukarobot_AssetInfo.json" -g $rg

az dt twin relationship create -n $dtName --kind "assetInformation"`
 --relationship-id "BodyMakerAsset0001-assetInformation-BodyMakerAsset0001_AssetInfo"`
 --source "Kuka_KR3_540_Agilus" --target "Kukarobot_AssetInfo" -g $rg

az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:Submodel;1"`
 --twin-id KukarobotOpsdata --properties ".\graph\KukaRobotOpsdata.json" -g $rg

az dt twin relationship create -n $dtName --kind "submodel"`
 --relationship-id "Kukarobot-submodel-OperationalData"`
 --source "Kuka_KR3_540_Agilus" --target "KukarobotOpsdata" -g $rg

# TODO: Add Property twins according to your OPC UA data tags