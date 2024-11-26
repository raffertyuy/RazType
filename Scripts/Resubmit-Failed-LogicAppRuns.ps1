############################################################
# Resubmit failed logic app runs
############################################################

# Sources:
# https://github.com/Azure/logicapps/tree/master/scripts/resubmit-all-failed-runs
# https://github.com/Azure/azure-powershell/issues/7752

# import-module Az

$tenantId = ""
$subscriptionId = ""
$resourceGroupName = ""
$logicAppName = ""

$startDateTime = "2024-02-02 17:00:00" # change these
$endDateTime = "2024-02-15 00:00:00"   # change these

#Clear-AzContext
#Connect-AzAccount -Subscription $subscriptionId -TenantId $tenantId

#Set-AzContext -SubscriptionId $subscriptionId -Tenant $tenantId

write-host "Fetching runs..."
$runs = Get-AzLogicAppRunHistory -FollowNextPageLink -ResourceGroupName $resourceGroupName -Name $logicAppName | where { $_.Status -eq 'Cancelled' -and $_.StartTime -gt $startDateTime -and $_.StartTime -lt $endDateTime }
$runsCount = $runs.Count
write-host "Found $runsCount runs"

$context = [Microsoft.Azure.Commands.Common.Authentication.Abstractions.AzureRmProfileProvider]::Instance.Profile.DefaultContext
$token = [Microsoft.Azure.Commands.Common.Authentication.AzureSession]::Instance.AuthenticationFactory.Authenticate($context.Account, $context.Environment, $context.Tenant.Id.ToString(), $null, [Microsoft.Azure.Commands.Common.Authentication.ShowDialog]::Never, $null, $dexResourceUrl).AccessToken
	
$headers = @{
	'Authorization' = 'Bearer ' + $token
}

$runCount = 0
$totalRuns = $runs.Count

$runCount = 0

Foreach($run in $runs) {
	$runCount++
	write-host "Running $runCount out of $totalRuns"

	$uri = 'https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Logic/workflows/{2}/triggers/{3}/histories/{4}/resubmit?api-version=2016-06-01' -f $subscriptionId, $resourceGroupName, $logicAppName, $run.Trigger.Name, $run.Name
	Invoke-RestMethod -Method 'POST' -Uri $uri -Headers $headers

	if ($runCount -lt $totalRuns) {
		# wait 100ms for every run, and 2 minutes for every 5 runs
		if ($runCount % 5 -eq 0) {
			Start-Sleep -Seconds 120
		} else {
			Start-Sleep -Milliseconds 100
		}
	}
}
