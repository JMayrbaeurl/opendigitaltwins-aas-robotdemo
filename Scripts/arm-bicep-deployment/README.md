# Building and deploying the solution

## Pre-requisites

- An Azure subscription
- Powershell and Azure command line installed

## Building artifacts

The solution is using Azure Function apps, that have to build and published to an Azure Container Registry first.

- Create a resource group for the build. E.g. 'tttech-work-rg'
- Run the 'buildAppAndPublish.ps1' Powershell script

## Deploying Azure services and configure the graph

- Create a resource group for the solution. E.g. 'tttech-nervedemo-rg' in West Europe
- Run the 'deployFull.ps1'

For more details take a look at the scripts.
