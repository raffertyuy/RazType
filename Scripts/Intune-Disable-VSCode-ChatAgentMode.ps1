# PowerShell script to disable VS Code Chat Agent Mode via registry
# Designed for Intune deployment

try {
    $policyPath = "HKLM:\SOFTWARE\Policies\Microsoft\VSCode"
    $logPath = "$env:TEMP\VSCode-ChatAgent-Disable.log"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    # Function to write to both console and log file
    function Write-Log {
        param($Message, $Level = "INFO")
        $logEntry = "[$timestamp] [$Level] $Message"
        Write-Output $logEntry
        Add-Content -Path $logPath -Value $logEntry -Force
    }
    
    Write-Log "Starting VS Code Chat Agent Mode disable process..."
    
    # Ensure the VSCode policy registry path exists
    If (!(Test-Path $policyPath)) {
        Write-Log "Creating registry path: $policyPath"
        New-Item -Path $policyPath -Force | Out-Null
    } else {
        Write-Log "Registry path already exists: $policyPath"
    }
    
    # Create/Update the ChatAgentMode policy value (DWORD). 0 = disabled, 1 = enabled.
    Write-Log "Setting ChatAgentMode to disabled (0)..."
    New-ItemProperty -Path $policyPath -Name "ChatAgentMode" -PropertyType DWORD -Value 0 -Force | Out-Null
    
    # Verify the registry change was successful
    $verifyValue = Get-ItemProperty -Path $policyPath -Name "ChatAgentMode" -ErrorAction Stop
    if ($verifyValue.ChatAgentMode -eq 0) {
        Write-Log "SUCCESS: VS Code Chat Agent Mode has been disabled"
        # Write to Windows Event Log for centralized monitoring
        Write-EventLog -LogName Application -Source "Application" -EventId 1000 -EntryType Information -Message "VS Code Chat Agent Mode disabled successfully via Intune script"
        exit 0
    } else {
        $errorMsg = "FAILED: Registry value was not set correctly. Expected: 0, Actual: $($verifyValue.ChatAgentMode)"
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