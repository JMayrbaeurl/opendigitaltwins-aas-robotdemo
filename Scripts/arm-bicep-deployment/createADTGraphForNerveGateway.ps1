param (
	[string] $dtName,
	[string] $rg = 'tttech-nervedemo-rg'
)

# First make the currently logged in user a 'Azure Digital Twins Data Owner'
$principalId = az ad signed-in-user show --query 'userPrincipalName' | ConvertFrom-Json
az dt role-assignment create -n $dtName --assignee $principalId --role "Azure Digital Twins Data Owner" -g $rg

# Next upload all models from the AAS ontology
az dt model create -n $dtName --from-directory .\Ontology -g $rg

# Then start with the core nodes of the Nerve Demo CNC machine shell
az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:AssetAdministrationShell;1"`
 --twin-id "nerve-demo-cnc-machine" --properties ".\graph\nerve-demo-cnc-machine.json" -g $rg

az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:AssetInformation;1"`
 --twin-id NerveDemoCNCMachine_AssetInfo --properties ".\graph\NerveDemoCNCMachine_AssetInfo.json" -g $rg

az dt twin relationship create -n $dtName --kind "assetInformation"`
 --relationship-id "nervedemocncmachine-assetInformation-NerveDemoCNCMachine_AssetInfo"`
 --source "nerve-demo-cnc-machine" --target "NerveDemoCNCMachine_AssetInfo" -g $rg

az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:Submodel;1"`
 --twin-id NerveDemoCNCMachineOpsdata --properties ".\graph\NerveDemoCNCMachineOpsdata.json" -g $rg

 az dt twin relationship create -n $dtName --kind "submodel"`
 --relationship-id "NerveDemoCNCMachine-submodel-OperationalData"`
 --source "nerve-demo-cnc-machine" --target "NerveDemoCNCMachineOpsdata" -g $rg

az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:Property;1"`
 --twin-id MachineError --properties ".\graph\NerveDemoCNCMachinePropMachineError.json" -g $rg

az dt twin relationship create -n $dtName --kind "submodelElement"`
 --relationship-id "NerveDemoCNCMachineOpsdata-submodelElement-MachineError"`
 --source "NerveDemoCNCMachineOpsdata" --target "MachineError" -g $rg

az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:Property;1"`
 --twin-id MachinePause --properties ".\graph\NerveDemoCNCMachinePropMachinePause.json" -g $rg

az dt twin relationship create -n $dtName --kind "submodelElement"`
 --relationship-id "NerveDemoCNCMachineOpsdata-submodelElement-MachinePause"`
 --source "NerveDemoCNCMachineOpsdata" --target "MachinePause" -g $rg

az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:Property;1"`
 --twin-id MachineStarted --properties ".\graph\NerveDemoCNCMachinePropMachineStarted.json" -g $rg

az dt twin relationship create -n $dtName --kind "submodelElement"`
 --relationship-id "NerveDemoCNCMachineOpsdata-submodelElement-MachineStarted"`
 --source "NerveDemoCNCMachineOpsdata" --target "MachineStarted" -g $rg

az dt twin create -n $dtName --dtmi "dtmi:digitaltwins:aas:Property;1"`
 --twin-id MeasuredDiameter --properties ".\graph\NerveDemoCNCMachinePropMeasuredDiameter.json" -g $rg

az dt twin relationship create -n $dtName --kind "submodelElement"`
 --relationship-id "NerveDemoCNCMachineOpsdata-submodelElement-MeasuredDiameter"`
 --source "NerveDemoCNCMachineOpsdata" --target "MeasuredDiameter" -g $rg