# Powershell script that lists through all the storage accounts in a subscription
# and enables the access key for each storage account.

# Connect-AzAccount

$storageAccounts = Get-AzStorageAccount
$results = @()

# Loop through each storage account and enable the access key
foreach ($storageAccount in $storageAccounts)
{
    Write-Host "Enabling access key for storage account: $($storageAccount.StorageAccountName)"
    $result = Set-AzStorageAccount -ResourceGroupName $storageAccount.ResourceGroupName -Name $storageAccount.StorageAccountName -AllowSharedKeyAccess $true -OutVariable output
    $results += $output
    $output | Format-Table -Property StorageAccountName, ResourceGroupName, AllowSharedKeyAccess -AutoSize
}

Write-Host "All storage account access keys enabled."
Write-Host "Summary of changes:"
$results | Format-Table -Property StorageAccountName, ResourceGroupName, AllowSharedKeyAccess -AutoSize