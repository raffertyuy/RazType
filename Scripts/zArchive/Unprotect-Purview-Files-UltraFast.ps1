# Unprotect-Purview-Files-UltraFast.ps1
# Ultra-optimized version for >100K files
# Focuses on speed over comprehensive detection
# Works with any folder path (OneDrive, SharePoint, local folders, etc.)
# Author: GitHub Copilot
# Date: August 5, 2025

param(
    [Parameter(Mandatory=$false)]
    [string]$Path = "$env:USERPROFILE\OneDrive\Temp",
    
    [Parameter(Mandatory=$false)]
    [string]$LogFile = "C:\Temp\Unprotect-UltraFast-Results.csv",
    
    [Parameter(Mandatory=$false)]
    [int]$ThrottleLimit = 50,  # Aggressive parallelism
    
    [Parameter(Mandatory=$false)]
    [int]$BatchSize = 10000,   # Large batches
    
    [Parameter(Mandatory=$false)]
    [int]$MaxFiles = 0,        # Limit processing (0 = no limit)
    
    [Parameter(Mandatory=$false)]
    [string[]]$OnlyExtensions = @(),  # Default to definitely protected extensions
    
    [Parameter(Mandatory=$false)]
    [switch]$FastMode,          # Skip all detection, process all files
    
    [Parameter(Mandatory=$false)]
    [switch]$ContentProtected  # Include files with content-level protection (like Purview)
)

# Define functions locally to avoid dependency issues
function Get-DefinitelyProtectedExtensions {
    return @('.ptxt', '.pxml', '.pjpg', '.pjpeg', '.ppdf', '.ppng', 
             '.ptif', '.ptiff', '.pbmp', '.pgif', '.pjpe', '.pjfif', '.pfile')
}

# Set default extensions if not provided
if ($OnlyExtensions.Count -eq 0) {
    $OnlyExtensions = Get-DefinitelyProtectedExtensions
}

# Function to write to log (minimal logging for speed)
function Write-FastLog {
    param([string]$Message)
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] $Message"
}

# Initialize CSV log file (minimal headers)
function Initialize-FastLogFile {
    param([string]$LogPath)
    "Timestamp,FilePath,Status" | Out-File -FilePath $LogPath -Encoding UTF8
}

# Enhanced file detection for content-level protection
function Test-ContentProtection {
    param([string]$FilePath)
    
    try {
        # Check for alternate data streams (Purview protection indicator)
        $streams = Get-Item $FilePath -Stream * -ErrorAction SilentlyContinue
        if ($streams.Count -gt 1) {
            $protectionStreams = $streams | Where-Object { 
                $_.Stream -match 'sec\.endpointdlp|msip|rms|protection|irm' 
            }
            if ($protectionStreams.Count -gt 0) {
                return $true
            }
        }
        
        # Quick header check for protection signatures
        $header = [System.IO.File]::ReadAllBytes($FilePath) | Select-Object -First 1024
        $headerString = [System.Text.Encoding]::ASCII.GetString($header)
        
        # Look for common protection markers
        if ($headerString -match 'MicrosoftIRMServices|Microsoft\.Information\.Protection|RMS|MSIP|Protected\s+(PDF|Document)') {
            return $true
        }
        
        return $false
    }
    catch {
        return $false
    }
}

# Thread-safe logging (streamlined)
function Add-FastResult {
    param([string]$FilePath, [string]$Status, [string]$LogFilePath)
    
    $entry = "$(Get-Date -Format 'HH:mm:ss'),`"$FilePath`",$Status"
    $mutex = New-Object System.Threading.Mutex($false, "UltraFastLogMutex")
    try {
        $mutex.WaitOne(1000) | Out-Null  # 1 second timeout
        $entry | Out-File -FilePath $LogFilePath -Append -Encoding UTF8
    }
    finally {
        $mutex.ReleaseMutex()
    }
}

# Ultra-fast file processing (removes protection without extensive checks)
function Remove-FileProtection {
    param([string]$FilePath, [string]$LogFilePath)
    
    try {
        # Method 1: Direct cipher decrypt (fastest)
        $process = Start-Process -FilePath "cipher.exe" -ArgumentList "/d", "`"$FilePath`"" -Wait -PassThru -NoNewWindow
        if ($process.ExitCode -eq 0) {
            Add-FastResult -FilePath $FilePath -Status "Decrypted" -LogFilePath $LogFilePath
            return $true
        }
        
        # Method 2: Quick copy-replace (if cipher fails)
        $tempFile = [System.IO.Path]::GetTempFileName()
        [System.IO.File]::Copy($FilePath, $tempFile, $true)
        
        # Quick validation - if copy is different size, it may have removed protection
        $original = [System.IO.FileInfo]::new($FilePath)
        $temp = [System.IO.FileInfo]::new($tempFile)
        
        if ($temp.Length -ne $original.Length -or $temp.Length -gt 0) {
            [System.IO.File]::Copy($tempFile, $FilePath, $true)
            [System.IO.File]::Delete($tempFile)
            Add-FastResult -FilePath $FilePath -Status "Replaced" -LogFilePath $LogFilePath
            return $true
        }
        
        [System.IO.File]::Delete($tempFile)
        Add-FastResult -FilePath $FilePath -Status "Failed" -LogFilePath $LogFilePath
        return $false
    }
    catch {
        Add-FastResult -FilePath $FilePath -Status "Error" -LogFilePath $LogFilePath
        return $false
    }
}

# Main execution
Write-FastLog "Starting Ultra-Fast File Unprotect"
Write-FastLog "Path: $Path | Threads: $ThrottleLimit | Batch: $BatchSize"

if (-not (Test-Path $Path)) {
    Write-Error "Path not found: $Path"
    exit 1
}

Initialize-FastLogFile -LogPath $LogFile

# Ultra-fast file discovery
Write-FastLog "Scanning files..."
$sw = [System.Diagnostics.Stopwatch]::StartNew()

if ($FastMode) {
    # Process ALL files in fast mode
    $allFiles = Get-ChildItem -Path $Path -Recurse -File -Force | Where-Object { $_.Length -gt 0 }
    Write-FastLog "Fast mode: Found $($allFiles.Count) files"
} elseif ($ContentProtected) {
    # Include both definitely protected extensions AND content-protected files
    $allFiles = @()
    
    # First, get files with definitely protected extensions
    foreach ($ext in $OnlyExtensions) {
        $files = Get-ChildItem -Path $Path -Recurse -File -Filter "*$ext" -Force
        $allFiles += $files
    }
    
    # Then, scan for content-protected files with common extensions
    $contentExtensions = @('.pdf', '.docx', '.xlsx', '.pptx', '.doc', '.xls', '.ppt', '.txt', '.jpg', '.png')
    foreach ($ext in $contentExtensions) {
        $candidates = Get-ChildItem -Path $Path -Recurse -File -Filter "*$ext" -Force
        foreach ($file in $candidates) {
            if (Test-ContentProtection $file.FullName) {
                $allFiles += $file
            }
        }
    }
    
    # Remove duplicates
    $allFiles = $allFiles | Sort-Object FullName | Get-Unique
    Write-FastLog "Content-protected mode: Found $($allFiles.Count) files"
} else {
    # Only process files with specific extensions (original behavior)
    $allFiles = @()
    foreach ($ext in $OnlyExtensions) {
        $files = Get-ChildItem -Path $Path -Recurse -File -Filter "*$ext" -Force
        $allFiles += $files
    }
    Write-FastLog "Extension filter: Found $($allFiles.Count) files"
}

if ($MaxFiles -gt 0 -and $allFiles.Count -gt $MaxFiles) {
    $allFiles = $allFiles | Select-Object -First $MaxFiles
    Write-FastLog "Limited to first $MaxFiles files"
}

$sw.Stop()
$totalFiles = $allFiles.Count
Write-FastLog "Scan completed in $($sw.Elapsed.TotalSeconds.ToString('F1'))s - Processing $totalFiles files"

if ($totalFiles -eq 0) {
    Write-FastLog "No files to process"
    exit 0
}

# Process in ultra-large batches
$processed = 0
$successful = 0
$startTime = Get-Date

for ($i = 0; $i -lt $totalFiles; $i += $BatchSize) {
    $batch = $allFiles[$i..([Math]::Min($i + $BatchSize - 1, $totalFiles - 1))]
    $batchNum = [Math]::Floor($i / $BatchSize) + 1
    
    Write-FastLog "Batch ${batchNum}: Processing $($batch.Count) files (files $($i+1) to $($i + $batch.Count))"
    
    # Ultra-parallel processing
    $results = $batch | ForEach-Object -Parallel {
        $file = $_
        $logFile = $using:LogFile
        
        # Import function into parallel scope
        function Add-FastResult {
            param([string]$FilePath, [string]$Status, [string]$LogFilePath)
            $entry = "$(Get-Date -Format 'HH:mm:ss'),`"$FilePath`",$Status"
            $mutex = New-Object System.Threading.Mutex($false, "UltraFastLogMutex")
            try {
                $mutex.WaitOne(1000) | Out-Null
                $entry | Out-File -FilePath $LogFilePath -Append -Encoding UTF8
            }
            finally {
                $mutex.ReleaseMutex()
            }
        }
        
        function Remove-FileProtection {
            param([string]$FilePath, [string]$LogFilePath)
            try {
                $process = Start-Process -FilePath "cipher.exe" -ArgumentList "/d", "`"$FilePath`"" -Wait -PassThru -NoNewWindow
                if ($process.ExitCode -eq 0) {
                    Add-FastResult -FilePath $FilePath -Status "Decrypted" -LogFilePath $LogFilePath
                    return $true
                }
                
                $tempFile = [System.IO.Path]::GetTempFileName()
                [System.IO.File]::Copy($FilePath, $tempFile, $true)
                
                $original = [System.IO.FileInfo]::new($FilePath)
                $temp = [System.IO.FileInfo]::new($tempFile)
                
                if ($temp.Length -ne $original.Length -or $temp.Length -gt 0) {
                    [System.IO.File]::Copy($tempFile, $FilePath, $true)
                    [System.IO.File]::Delete($tempFile)
                    Add-FastResult -FilePath $FilePath -Status "Replaced" -LogFilePath $LogFilePath
                    return $true
                }
                
                [System.IO.File]::Delete($tempFile)
                Add-FastResult -FilePath $FilePath -Status "Failed" -LogFilePath $LogFilePath
                return $false
            }
            catch {
                Add-FastResult -FilePath $FilePath -Status "Error" -LogFilePath $LogFilePath
                return $false
            }
        }
        
        # Process the file
        Remove-FileProtection -FilePath $file.FullName -LogFilePath $logFile
        
    } -ThrottleLimit $ThrottleLimit
    
    # Update counters
    $processed += $batch.Count
    $successful += ($results | Where-Object { $_ -eq $true }).Count
    
    $elapsed = (Get-Date) - $startTime
    $rate = [Math]::Round($processed / $elapsed.TotalSeconds, 1)
    $percentDone = [Math]::Round(($processed / $totalFiles) * 100, 1)
    
    Write-FastLog "Batch $batchNum complete: $processed/$totalFiles ($percentDone%) | Rate: $rate files/sec"
    
    # Minimal memory management
    if ($batchNum % 10 -eq 0) {
        [System.GC]::Collect()
    }
}

$totalTime = (Get-Date) - $startTime
$avgRate = [Math]::Round($totalFiles / $totalTime.TotalSeconds, 1)

Write-FastLog "=== ULTRA-FAST COMPLETION ==="
Write-FastLog "Total files: $totalFiles"
Write-FastLog "Processed: $processed"
Write-FastLog "Success rate: $(if($processed -gt 0){[Math]::Round(($successful/$processed)*100,1)}else{0})%"
Write-FastLog "Total time: $($totalTime.TotalMinutes.ToString('F1')) minutes"
Write-FastLog "Average rate: $avgRate files/second"
Write-FastLog "Log: $LogFile"

# Summary to log
$summary = "$(Get-Date -Format 'HH:mm:ss'),`"SUMMARY: $totalFiles files, $successful successful, $($totalTime.TotalMinutes.ToString('F1'))min, ${avgRate}fps`",Complete"
$summary | Out-File -FilePath $LogFile -Append -Encoding UTF8
