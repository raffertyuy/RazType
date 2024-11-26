$logicAppName = 'razhandwritingtomarkdown'
$rgName = 'raz-workflows-rg'
$fileName = "$($logicAppName).json"

$subscriptionId = ''

# $parameters = @{
#    Token = (az account get-access-token | ConvertFrom-Json).accessToken
#    LogicApp = $logicAppName
#    ResourceGroup = 'raz-workflows-rg'
#    SubscriptionId = $subscriptionId
#    Verbose = $true
#}

#Get-LogicAppTemplate $parameters | Out-File $fileName
#Get-ParameterTemplate -TemplateFile $fileName | Out-File "$($logicAppName).param.json"

Get-LogicAppTemplate -LogicApp $logicAppName -ResourceGroup $rgName -SubscriptionId $subscriptionId | Out-File $fileName

