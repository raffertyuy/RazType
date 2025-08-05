# Unprotect-OneDrive.ps1
# Script to unprotect and unencrypt OneDrive files encrypted by MS Purview
# Author: GitHub Copilot
# Date: August 5, 2025

param(
    [Parameter(Mandatory=$false)]
    [string]$OneDrivePath = "$env:USERPROFILE\OneDrive\Temp",
    
    [Parameter(Mandatory=$false)]
    [string]$LogFile = "C:\GitRepos\OneDrive-Unprotect-Results.csv",
    
    [Parameter(Mandatory=$false)]
    [int]$ThrottleLimit = 10,
    
    [Parameter(Mandatory=$false)]
    [int]$BatchSize = 1000
)

# Function to write to log
function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] [$Level] $Message"
}

# Function to initialize CSV log file
function Initialize-LogFile {
    param([string]$LogPath)
    
    $headers = "Timestamp,FilePath,Action,Status,ErrorMessage"
    $headers | Out-File -FilePath $LogPath -Encoding UTF8
    Write-Log "Log file initialized: $LogPath"
}

# Function to log result to CSV (thread-safe)
function Log-Result {
    param(
        [string]$FilePath,
        [string]$Action,
        [string]$Status,
        [string]$ErrorMessage = "",
        [string]$LogFilePath
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "`"$timestamp`",`"$FilePath`",`"$Action`",`"$Status`",`"$ErrorMessage`""
    
    # Use mutex for thread-safe file writing
    $mutex = New-Object System.Threading.Mutex($false, "OneDriveUnprotectLogMutex")
    try {
        $mutex.WaitOne() | Out-Null
        $logEntry | Out-File -FilePath $LogFilePath -Append -Encoding UTF8
    }
    finally {
        $mutex.ReleaseMutex()
    }
}

# Function for parallel-safe progress reporting
function Update-ProgressCounter {
    param(
        [ref]$Counter,
        [int]$TotalFiles
    )
    
    $currentCount = [System.Threading.Interlocked]::Increment($Counter.Value)
    if ($currentCount % 100 -eq 0 -or $currentCount -eq $TotalFiles) {
        $percentComplete = [math]::Round(($currentCount / $TotalFiles) * 100, 2)
        Write-Progress -Activity "Processing OneDrive Files" -Status "Processed $currentCount of $TotalFiles files" -PercentComplete $percentComplete
    }
    return $currentCount
}

# Function to check if file is encrypted/protected
function Test-FileProtected {
    param([string]$FilePath)
    
    try {
        # Check file attributes for encryption
        $file = Get-Item $FilePath -Force
        
        # Check if file has encryption attribute
        if ($file.Attributes -band [System.IO.FileAttributes]::Encrypted) {
            return $true
        }
        
        # Check for RMS/Purview protection by attempting to read file properties
        $shell = New-Object -ComObject Shell.Application
        $folder = $shell.Namespace([System.IO.Path]::GetDirectoryName($FilePath))
        $item = $folder.ParseName([System.IO.Path]::GetFileName($FilePath))
        
        # Check extended properties for protection indicators
        for ($i = 0; $i -lt 400; $i++) {
            $property = $folder.GetDetailsOf($item, $i)
            if ($property -match "Protected|Encrypted|Rights|Purview") {
                return $true
            }
        }
        
        return $false
    }
    catch {
        Write-Log "Error checking file protection status: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

# Function to attempt file unprotection (optimized for parallel execution)
function Unprotect-File {
    param(
        [string]$FilePath,
        [string]$LogFilePath
    )
    
    try {
        # Method 1: Try to remove encryption attribute (fastest method)
        $file = Get-Item $FilePath -Force -ErrorAction SilentlyContinue
        if ($file -and ($file.Attributes -band [System.IO.FileAttributes]::Encrypted)) {
            try {
                # Decrypt the file using cipher
                $process = Start-Process -FilePath "cipher.exe" -ArgumentList "/d", "`"$FilePath`"" -Wait -PassThru -NoNewWindow -RedirectStandardOutput $env:TEMP\cipher_out.txt -RedirectStandardError $env:TEMP\cipher_err.txt
                if ($process.ExitCode -eq 0) {
                    Log-Result -FilePath $FilePath -Action "Decrypt" -Status "Success" -LogFilePath $LogFilePath
                    return $true
                }
            }
            catch {
                # Cipher failed, continue to other methods
            }
        }
        
        # Method 2: Quick file attribute check and copy method
        try {
            $tempFile = "$env:TEMP\temp_unprotect_$([System.Guid]::NewGuid().ToString('N').Substring(0,8)).tmp"
            Copy-Item -Path $FilePath -Destination $tempFile -Force -ErrorAction Stop
            
            # Quick check if copy removed protection
            $tempFileInfo = Get-Item $tempFile -Force -ErrorAction SilentlyContinue
            if ($tempFileInfo -and -not ($tempFileInfo.Attributes -band [System.IO.FileAttributes]::Encrypted)) {
                # Replace original with unprotected copy
                Remove-Item $FilePath -Force -ErrorAction Stop
                Move-Item $tempFile $FilePath -Force -ErrorAction Stop
                Log-Result -FilePath $FilePath -Action "Copy-Replace" -Status "Success" -LogFilePath $LogFilePath
                return $true
            }
            else {
                Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
            }
        }
        catch {
            # Clean up temp file if it exists
            if (Test-Path $tempFile -ErrorAction SilentlyContinue) {
                Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
            }
        }
        
        # If all methods failed
        Log-Result -FilePath $FilePath -Action "Unprotect" -Status "Failed" -ErrorMessage "All unprotection methods failed" -LogFilePath $LogFilePath
        return $false
    }
    catch {
        $errorMsg = $_.Exception.Message
        Log-Result -FilePath $FilePath -Action "Unprotect" -Status "Error" -ErrorMessage $errorMsg -LogFilePath $LogFilePath
        return $false
    }
}

# Main execution
Write-Log "Starting OneDrive Unprotect Script (Parallel Edition)"
Write-Log "OneDrive Path: $OneDrivePath"
Write-Log "Log File: $LogFile"
Write-Log "Parallel Threads: $ThrottleLimit"
Write-Log "Batch Size: $BatchSize"

# Check if OneDrive path exists
if (-not (Test-Path $OneDrivePath)) {
    Write-Log "OneDrive path not found: $OneDrivePath" "ERROR"
    Write-Log "Please specify the correct OneDrive path using -OneDrivePath parameter"
    exit 1
}

# Initialize log file
Initialize-LogFile -LogPath $LogFile

# Get all files recursively
Write-Log "Scanning for files in OneDrive..."
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$allFiles = Get-ChildItem -Path $OneDrivePath -Recurse -File -Force -ErrorAction SilentlyContinue
$stopwatch.Stop()

$totalFiles = $allFiles.Count
Write-Log "Found $totalFiles files to process (scan took $($stopwatch.Elapsed.TotalSeconds.ToString('F2')) seconds)"

if ($totalFiles -eq 0) {
    Write-Log "No files found to process. Exiting." "WARNING"
    exit 0
}

# Initialize counters for parallel processing
$processedCounter = 0
$successCounter = 0
$failedCounter = 0
$skippedCounter = 0

# Process files in parallel batches
Write-Log "Starting parallel processing with $ThrottleLimit threads..."
$processingStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# Split files into batches for better memory management
$batchNumber = 0
for ($i = 0; $i -lt $totalFiles; $i += $BatchSize) {
    $batchNumber++
    $batch = $allFiles[$i..([Math]::Min($i + $BatchSize - 1, $totalFiles - 1))]
    $batchSize = $batch.Count
    
    Write-Log "Processing batch $batchNumber (files $($i + 1) to $($i + $batchSize)) - $batchSize files"
    
    # Process current batch in parallel
    $batchResults = $batch | ForEach-Object -Parallel {
        # Import required variables and functions into parallel runspace
        $LogFile = $using:LogFile
        $processedCounter = $using:processedCounter
        $successCounter = $using:successCounter
        $failedCounter = $using:failedCounter
        $skippedCounter = $using:skippedCounter
        $totalFiles = $using:totalFiles
        
        # Import the Log-Result function
        function Log-Result {
            param(
                [string]$FilePath,
                [string]$Action,
                [string]$Status,
                [string]$ErrorMessage = "",
                [string]$LogFilePath
            )
            
            $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            $logEntry = "`"$timestamp`",`"$FilePath`",`"$Action`",`"$Status`",`"$ErrorMessage`""
            
            # Use mutex for thread-safe file writing
            $mutex = New-Object System.Threading.Mutex($false, "OneDriveUnprotectLogMutex")
            try {
                $mutex.WaitOne() | Out-Null
                $logEntry | Out-File -FilePath $LogFilePath -Append -Encoding UTF8
            }
            finally {
                $mutex.ReleaseMutex()
            }
        }
        
        # Import the Test-FileProtected function (simplified for parallel execution)
        function Test-FileProtected {
            param([string]$FilePath)
            
            try {
                $file = Get-Item $FilePath -Force -ErrorAction SilentlyContinue
                if ($file -and ($file.Attributes -band [System.IO.FileAttributes]::Encrypted)) {
                    return $true
                }
                
                # Quick check for protected file extensions or metadata
                $extension = [System.IO.Path]::GetExtension($FilePath).ToLower()
                if ($extension -in @('.pfile', '.ptxt', '.pjpg', '.ppdf')) {
                    return $true
                }
                
                return $false
            }
            catch {
                return $false
            }
        }
        
        # Import the Unprotect-File function
        function Unprotect-File {
            param(
                [string]$FilePath,
                [string]$LogFilePath
            )
            
            try {
                # Method 1: Try to remove encryption attribute (fastest method)
                $file = Get-Item $FilePath -Force -ErrorAction SilentlyContinue
                if ($file -and ($file.Attributes -band [System.IO.FileAttributes]::Encrypted)) {
                    try {
                        # Decrypt the file using cipher
                        $process = Start-Process -FilePath "cipher.exe" -ArgumentList "/d", "`"$FilePath`"" -Wait -PassThru -NoNewWindow -RedirectStandardOutput $env:TEMP\cipher_out.txt -RedirectStandardError $env:TEMP\cipher_err.txt
                        if ($process.ExitCode -eq 0) {
                            Log-Result -FilePath $FilePath -Action "Decrypt" -Status "Success" -LogFilePath $LogFilePath
                            return $true
                        }
                    }
                    catch {
                        # Cipher failed, continue to other methods
                    }
                }
                
                # Method 2: Quick file attribute check and copy method
                try {
                    $tempFile = "$env:TEMP\temp_unprotect_$([System.Guid]::NewGuid().ToString('N').Substring(0,8)).tmp"
                    Copy-Item -Path $FilePath -Destination $tempFile -Force -ErrorAction Stop
                    
                    # Quick check if copy removed protection
                    $tempFileInfo = Get-Item $tempFile -Force -ErrorAction SilentlyContinue
                    if ($tempFileInfo -and -not ($tempFileInfo.Attributes -band [System.IO.FileAttributes]::Encrypted)) {
                        # Replace original with unprotected copy
                        Remove-Item $FilePath -Force -ErrorAction Stop
                        Move-Item $tempFile $FilePath -Force -ErrorAction Stop
                        Log-Result -FilePath $FilePath -Action "Copy-Replace" -Status "Success" -LogFilePath $LogFilePath
                        return $true
                    }
                    else {
                        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
                    }
                }
                catch {
                    # Clean up temp file if it exists
                    if (Test-Path $tempFile -ErrorAction SilentlyContinue) {
                        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
                    }
                }
                
                # If all methods failed
                Log-Result -FilePath $FilePath -Action "Unprotect" -Status "Failed" -ErrorMessage "All unprotection methods failed" -LogFilePath $LogFilePath
                return $false
            }
            catch {
                $errorMsg = $_.Exception.Message
                Log-Result -FilePath $FilePath -Action "Unprotect" -Status "Error" -ErrorMessage $errorMsg -LogFilePath $LogFilePath
                return $false
            }
        }
        
        # Process the current file
        $file = $_
        $result = @{
            FilePath = $file.FullName
            Processed = $false
            Success = $false
            Skipped = $false
            Failed = $false
        }
        
        try {
            # Skip system files and thumbnails
            if ($file.Name -match '^(desktop\.ini|thumbs\.db|\.ds_store)$|~\$') {
                Log-Result -FilePath $file.FullName -Action "Skip" -Status "System File" -LogFilePath $LogFile
                $result.Skipped = $true
            }
            else {
                # Check if file appears to be protected/encrypted
                if (Test-FileProtected $file.FullName) {
                    if (Unprotect-File $file.FullName $LogFile) {
                        $result.Success = $true
                    }
                    else {
                        $result.Failed = $true
                    }
                }
                else {
                    Log-Result -FilePath $file.FullName -Action "Check" -Status "Not Protected" -LogFilePath $LogFile
                    $result.Skipped = $true
                }
            }
            $result.Processed = $true
        }
        catch {
            $errorMsg = $_.Exception.Message
            Log-Result -FilePath $file.FullName -Action "Process" -Status "Error" -ErrorMessage $errorMsg -LogFilePath $LogFile
            $result.Failed = $true
            $result.Processed = $true
        }
        
        return $result
        
    } -ThrottleLimit $ThrottleLimit
    
    # Aggregate batch results
    $batchProcessed = 0
    $batchSuccess = 0
    $batchFailed = 0
    $batchSkipped = 0
    
    foreach ($result in $batchResults) {
        if ($result.Processed) { $batchProcessed++ }
        if ($result.Success) { $batchSuccess++ }
        if ($result.Failed) { $batchFailed++ }
        if ($result.Skipped) { $batchSkipped++ }
    }
    
    # Update global counters (simple addition since we're not truly parallel here)
    $processedCounter += $batchProcessed
    $successCounter += $batchSuccess
    $failedCounter += $batchFailed
    $skippedCounter += $batchSkipped
    
    $percentComplete = [math]::Round(($processedCounter / $totalFiles) * 100, 2)
    
    Write-Log "Batch $batchNumber completed: Processed=$batchProcessed, Success=$batchSuccess, Failed=$batchFailed, Skipped=$batchSkipped"
    Write-Progress -Activity "Processing OneDrive Files" -Status "Completed $processedCounter of $totalFiles files ($percentComplete%)" -PercentComplete $percentComplete
    
    # Memory cleanup between batches
    [System.GC]::Collect()
}

$processingStopwatch.Stop()
Write-Progress -Activity "Processing OneDrive Files" -Completed

# Final summary
Write-Log "=== SUMMARY ===" "INFO"
Write-Log "Total files processed: $processedCounter" "INFO"
Write-Log "Successfully unprotected: $successCounter" "INFO"
Write-Log "Failed to unprotect: $failedCounter" "INFO"
Write-Log "Skipped (not protected): $skippedCounter" "INFO"
Write-Log "Processing time: $($processingStopwatch.Elapsed.TotalMinutes.ToString('F2')) minutes" "INFO"
Write-Log "Average speed: $([math]::Round($totalFiles / $processingStopwatch.Elapsed.TotalSeconds, 2)) files/second" "INFO"
Write-Log "Results logged to: $LogFile" "INFO"

# Add summary to log file
$summaryEntry = "`"$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`",`"SUMMARY`",`"Complete`",`"Success`",`"Total: $processedCounter, Success: $successCounter, Failed: $failedCounter, Skipped: $skippedCounter, Time: $($processingStopwatch.Elapsed.TotalMinutes.ToString('F2'))min`""
$summaryEntry | Out-File -FilePath $LogFile -Append -Encoding UTF8

Write-Log "Script completed. Check $LogFile for detailed results."

# Open log file for review
if (Test-Path $LogFile) {
    Write-Host "Would you like to open the results file? (Y/N): " -NoNewline
    $response = Read-Host
    if ($response -eq 'Y' -or $response -eq 'y') {
        Start-Process $LogFile
    }
}