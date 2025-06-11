# PowerShell script to disable VS Code Copilot settings via registry
# Designed for Intune deployment

try {
    $policyPath = "HKLM:\SOFTWARE\Policies\Microsoft\VSCode"
    $logPath = "$env:TEMP\VSCode-Copilot-Disable.log"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    # VS Code Copilot settings to configure (0 = disabled, 1 = enabled)
    # Comment out any setting below to skip configuring it
    # Modify the Value property to change the setting (0 = disabled, 1 = enabled)
    $copilotSettings = @(
        @{ Name = "ChatAgentMode"; Description = "Chat Agent Mode"; Value = 0 }
        @{ Name = "ChatMCP"; Description = "Chat MCP"; Value = 0 }
        @{ Name = "ChatToolsAutoApprove"; Description = "Chat Tools Auto Approve"; Value = 0 }
    )
    
    # Function to write to both console and log file
    function Write-Log {
        param($Message, $Level = "INFO")
        $logEntry = "[$timestamp] [$Level] $Message"
        Write-Output $logEntry
        Add-Content -Path $logPath -Value $logEntry -Force
    }
    
    Write-Log "Starting VS Code Copilot settings configuration process..."
    
    # Ensure the VSCode policy registry path exists
    if (!(Test-Path $policyPath)) {
        Write-Log "Creating registry path: $policyPath"
        New-Item -Path $policyPath -Force | Out-Null
    } else {
        Write-Log "Registry path already exists: $policyPath"
    }
    
    # Apply each Copilot setting
    $allSuccessful = $true
    foreach ($setting in $copilotSettings) {
        $statusText = if ($setting.Value -eq 0) { "disabled" } else { "enabled" }
        Write-Log "Setting $($setting.Description) to $statusText ($($setting.Value))..."
        New-ItemProperty -Path $policyPath -Name $setting.Name -PropertyType DWORD -Value $setting.Value -Force | Out-Null
        
        # Verify the registry change
        $verifyValue = Get-ItemProperty -Path $policyPath -Name $setting.Name -ErrorAction Stop
        if ($verifyValue.$($setting.Name) -eq $setting.Value) {
            Write-Log "SUCCESS: $($setting.Description) has been set to $statusText"
        } else {
            $errorMsg = "FAILED: $($setting.Name) was not set correctly. Expected: $($setting.Value), Actual: $($verifyValue.$($setting.Name))"
            Write-Log $errorMsg "ERROR"
            $allSuccessful = $false
        }
    }
    
    # Final result
    if ($allSuccessful) {
        $successMsg = "All VS Code Copilot settings configured successfully via Intune script"
        Write-Log $successMsg
        Write-EventLog -LogName Application -Source "Application" -EventId 1000 -EntryType Information -Message $successMsg
        exit 0
    } else {
        $errorMsg = "One or more VS Code Copilot settings failed to configure properly"
        Write-Log $errorMsg "ERROR"
        Write-EventLog -LogName Application -Source "Application" -EventId 1001 -EntryType Error -Message $errorMsg
        exit 1
    }
}
catch {
    $errorMsg = "FAILED: An error occurred - $($_.Exception.Message). Log file: $logPath"
    Write-Error $errorMsg
    Add-Content -Path $logPath -Value "[$timestamp] [ERROR] $errorMsg" -Force
    Write-EventLog -LogName Application -Source "Application" -EventId 1002 -EntryType Error -Message $errorMsg
    exit 1
}