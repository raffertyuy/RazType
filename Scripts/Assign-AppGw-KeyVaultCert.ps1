Connect-AzureAD -Tenant <TenantID>

# Get the Application Gateway we want to modify
$appgw = Get-AzApplicationGateway -Name razopenai-appgw -ResourceGroupName openai-rg
# Specify the resource id to the user assigned managed identity - This can be found by going to the properties of the managed identity


Set-AzApplicationGatewayIdentity -ApplicationGateway $appgw -UserAssignedIdentityId "/subscriptions/<SubscriptionID>/resourceGroups/common-eus-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/razeusmi"
# Get the secret ID from Key Vault
$secret = Get-AzKeyVaultSecret -VaultName "razvault" -Name "razopenai-appgw"
$secretId = $secret.Id.Replace($secret.Version, "") # Remove the secret version so AppGW will use the latest version in future syncs
# Specify the secret ID from Key Vault 
Add-AzApplicationGatewaySslCertificate -KeyVaultSecretId $secretId -ApplicationGateway $appgw -Name $secret.Name
# Commit the changes to the Application Gateway
Set-AzApplicationGateway -ApplicationGateway $appgw