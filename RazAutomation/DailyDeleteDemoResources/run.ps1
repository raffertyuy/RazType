# Input bindings are passed in via param block.
param($Timer)

# Import Az Module
Import-Module Az.Resources

# Delete resources with naming conventions like 'prefix-demo-deleteme'
$resources = Get-AzResourceGroup | Where-Object ResourceGroupName -like '*-deleteme'

Foreach ($resource in $resources)
{
    Write-Host "Deleting $($resource.ResourceGroupName)..."
    Remove-AzResourceGroup -Name $resource.ResourceGroupName -Force
}

Write-Host "Done deleting all '-deleteme' Resource Groups..."