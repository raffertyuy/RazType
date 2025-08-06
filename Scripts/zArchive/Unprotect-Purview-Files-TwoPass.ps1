# Unprotect-Purview-Files-TwoPass-Simplified.ps1
# Simplified two-pass strategy - leverages existing scripts without duplication
# Works with any folder path (OneDrive, SharePoint, local folders, etc.)
# Author: GitHub Copilot
# Date: August 5, 2025

param(
    [Parameter(Mandatory=$false)]
    [string]$Path = "$env:USERPROFILE\OneDrive\Temp",
    
    [Parameter(Mandatory=$false)]
    [string]$LogFolder = "C:\Temp",
    
    [Parameter(Mandatory=$false)]
    [int]$UltraFastThreads = 50,
    
    [Parameter(Mandatory=$false)]
    [int]$NormalThreads = 25,
    
    [Parameter(Mandatory=$false)]
    [int]$BatchSize = 0,     # 0 = use script defaults
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipPass2    # Skip second pass if you only want definitely protected files
)

# Simple logging function
function Write-SimpleLog {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $Message"
}

Write-SimpleLog "=== TWO-PASS UNPROTECT STRATEGY ==="
Write-SimpleLog "Target Path: $Path"

# Validate paths
if (-not (Test-Path $Path)) {
    Write-Error "ERROR: Target path not found: $Path"
    exit 1
}

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$ultraFastScript = Join-Path $scriptPath "Unprotect-Purview-Files-UltraFast.ps1"
$normalScript = Join-Path $scriptPath "Unprotect-Purview-Files.ps1"

if (-not (Test-Path $ultraFastScript)) {
    Write-Error "ERROR: UltraFast script not found: $ultraFastScript"
    exit 1
}

if (-not (Test-Path $normalScript)) {
    Write-Error "ERROR: Normal script not found: $normalScript"
    exit 1
}

# Generate timestamped log files
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$pass1Log = "$LogFolder\Unprotect-Pass1-UltraFast-$timestamp.csv"
$pass2Log = "$LogFolder\Unprotect-Pass2-Normal-$timestamp.csv"

$totalStartTime = Get-Date

# =================================================================
# PASS 1: ULTRA-FAST - Definitely Protected Files
# =================================================================
Write-SimpleLog ""
Write-SimpleLog "=== PASS 1: ULTRA-FAST PROCESSING ==="
Write-SimpleLog "Target: Files that are DEFINITELY protected (.pfile, .ptxt, .ppdf, etc.)"
Write-SimpleLog "Log: $pass1Log"

$pass1StartTime = Get-Date

# Use UltraFast script with its own defaults (no extension override needed)
if ($BatchSize -gt 0) {
    & $ultraFastScript -Path $Path -LogFile $pass1Log -ThrottleLimit $UltraFastThreads -BatchSize $BatchSize
} else {
    & $ultraFastScript -Path $Path -LogFile $pass1Log -ThrottleLimit $UltraFastThreads
}
$pass1ExitCode = $LASTEXITCODE

$pass1Duration = (Get-Date) - $pass1StartTime
Write-SimpleLog "Pass 1 completed in $($pass1Duration.TotalMinutes.ToString('F1')) minutes (Exit Code: $pass1ExitCode)"

if ($SkipPass2) {
    Write-SimpleLog "Skipping Pass 2 as requested (-SkipPass2 flag)"
    Write-SimpleLog "=== TWO-PASS STRATEGY COMPLETED (PASS 1 ONLY) ==="
    Write-SimpleLog "Results: $pass1Log"
    exit $pass1ExitCode
}

# =================================================================
# PASS 2: NORMAL SCRIPT - Potentially Protected Files  
# =================================================================
Write-SimpleLog ""
Write-SimpleLog "=== PASS 2: COMPREHENSIVE PROCESSING ==="
Write-SimpleLog "Target: Files that COULD be protected (Office docs, PDFs, images, etc.)"
Write-SimpleLog "Mode: QuickScan with default extensions (excludes Pass 1 extensions automatically)"
Write-SimpleLog "Log: $pass2Log"

$pass2StartTime = Get-Date

# Use Normal script with QuickScan - it will automatically exclude already protected extensions
if ($BatchSize -gt 0) {
    & $normalScript -Path $Path -LogFile $pass2Log -ThrottleLimit $NormalThreads -BatchSize $BatchSize -QuickScan
} else {
    & $normalScript -Path $Path -LogFile $pass2Log -ThrottleLimit $NormalThreads -QuickScan
}
$pass2ExitCode = $LASTEXITCODE

$pass2Duration = (Get-Date) - $pass2StartTime
Write-SimpleLog "Pass 2 completed in $($pass2Duration.TotalMinutes.ToString('F1')) minutes (Exit Code: $pass2ExitCode)"

# =================================================================
# FINAL SUMMARY
# =================================================================
$totalDuration = (Get-Date) - $totalStartTime

Write-SimpleLog ""
Write-SimpleLog "=== TWO-PASS STRATEGY COMPLETED ==="
Write-SimpleLog "Total Time: $($totalDuration.TotalMinutes.ToString('F1')) minutes"
Write-SimpleLog "Pass 1 Time: $($pass1Duration.TotalMinutes.ToString('F1')) minutes"
Write-SimpleLog "Pass 2 Time: $($pass2Duration.TotalMinutes.ToString('F1')) minutes"
Write-SimpleLog ""
Write-SimpleLog "DETAILED RESULTS:"
Write-SimpleLog "- Pass 1 (UltraFast): $pass1Log"
Write-SimpleLog "- Pass 2 (Normal): $pass2Log"

# Simple exit code logic
$overallExitCode = [Math]::Max($pass1ExitCode, $pass2ExitCode)
if ($overallExitCode -eq 0) {
    Write-SimpleLog "✅ Two-pass strategy completed successfully!"
} else {
    Write-SimpleLog "⚠️  Two-pass strategy completed with errors. Check individual logs for details."
}

exit $overallExitCode
