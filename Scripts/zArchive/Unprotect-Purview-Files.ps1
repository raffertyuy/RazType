# Unprotect-Purview-Files.ps1
# Script to unprotect and unencrypt files encrypted by MS Purview/Information Protection
# Works with any folder path (OneDrive, SharePoint, local folders, etc.)
# Author: GitHub Copilot
# Date: August 5, 2025

param(
    [Parameter(Mandatory=$false)]
    [string]$Path = "$env:USERPROFILE\OneDrive\Temp",
    
    [Parameter(Mandatory=$false)]
    [string]$LogFile = "C:\Temp\Unprotect-Results.csv",
    
    [Parameter(Mandatory=$false)]
    [int]$ThrottleLimit = 25,  # Increased for large datasets
    
    [Parameter(Mandatory=$false)]
    [int]$BatchSize = 5000,    # Larger batches for efficiency
    
    [Parameter(Mandatory=$false)]
    [switch]$QuickScan,        # Skip deep protection checks
    
    [Parameter(Mandatory=$false)]
    [string[]]$IncludeExtensions = @(),  # Allow custom extension filtering
    
    [Parameter(Mandatory=$false)]
    [int]$MinFileSizeKB = 1,   # Skip files smaller than this (KB)
    
    [Parameter(Mandatory=$false)]
    [switch]$FunctionOnly      # Only load functions (for dot-sourcing)
)

# Define extension lists for reuse across scripts
function Get-DefinitelyProtectedExtensions {
    return @('.ptxt', '.pxml', '.pjpg', '.pjpeg', '.ppdf', '.ppng', 
             '.ptif', '.ptiff', '.pbmp', '.pgif', '.pjpe', '.pjfif', '.pfile')
}

function Get-SupportedExtensions {
    return @('.docx', '.doc', '.xlsx', '.xls', '.pptx', '.ppt', '.pdf', '.txt', '.rtf', 
             '.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tif', '.tiff', '.svg', '.webp', 
             '.mp4', '.avi', '.mov', '.wmv', '.flv', '.mkv', '.mp3', '.wav', '.aac', 
             '.xml', '.json', '.csv', '.html', '.htm', '.js', '.css', '.py', '.java', 
             '.c', '.cpp', '.h', '.cs', '.vb', '.sql', '.sh', '.bat', '.ps1', '.php', 
             '.rb', '.go', '.swift', '.kt', '.ts', '.jsx', '.vue', '.r', '.m', '.scala', 
             '.pl', '.lua', '.dart', '.rust', '.asm', '.f90', '.cob', '.ada', '.pas', 
             '.eml', '.msg', '.pst', '.ost', '.vcf', '.ics', '.zip', '.rar', '.7z', 
             '.tar', '.gz', '.bz2', '.xz', '.iso', '.dmg', '.pkg', '.deb', '.rpm', 
             '.msi', '.exe', '.app', '.apk', '.ipa', '.jar', '.war', '.ear')
}

# Exit early if only loading functions (for dot-sourcing)
if ($FunctionOnly) { return }

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

# Function to check if file is encrypted/protected (enhanced for Purview detection)
function Test-FileProtected {
    param([string]$FilePath)
    
    try {
        # Fast path: Check file extension first (already protected extensions)
        $extension = [System.IO.Path]::GetExtension($FilePath).ToLower()
        
        # Files that are already protected (native protection with changed extensions)
        $protectedExtensions = @('.ptxt', '.pxml', '.pjpg', '.pjpeg', '.ppdf', '.ppng', 
                                '.ptif', '.ptiff', '.pbmp', '.pgif', '.pjpe', '.pjfif', '.pfile')
        
        if ($extension -in $protectedExtensions) {
            return $true
        }
        
        # Fast path: Check file size (skip very small files that are unlikely to be protected)
        $fileInfo = [System.IO.FileInfo]::new($FilePath)
        if ($fileInfo.Length -lt 1024) {  # Skip files smaller than 1KB
            return $false
        }
        
        # Fast encryption attribute check
        if ($fileInfo.Attributes -band [System.IO.FileAttributes]::Encrypted) {
            return $true
        }
        
        # ENHANCED: Check for alternate data streams (common with Purview protection)
        try {
            $streams = Get-Item $FilePath -Stream * -ErrorAction SilentlyContinue
            if ($streams.Count -gt 1) {
                # Look for specific protection-related streams
                $protectionStreams = $streams | Where-Object { 
                    $_.Stream -match 'sec\.endpointdlp|msip|rms|protection|irm' 
                }
                if ($protectionStreams.Count -gt 0) {
                    return $true
                }
            }
        }
        catch {
            # Continue to other checks if stream check fails
        }
        
        # ENHANCED: More comprehensive file header check for protection signatures
        try {
            $header = [System.IO.File]::ReadAllBytes($FilePath) | Select-Object -First 2048  # Read more bytes
            $headerString = [System.Text.Encoding]::ASCII.GetString($header)
            $headerUtf8 = [System.Text.Encoding]::UTF8.GetString($header)
            
            # Enhanced protection signatures (case-insensitive)
            $protectionPatterns = @(
                'Microsoft\.Information\.Protection',
                'MicrosoftIRMServices',
                'Purview',
                'RMS',
                'Rights.*Management',
                'MSIP',
                'OfficeIRM',
                'Protected\s+(PDF|Document)',
                'DRMContent',
                'sec\.endpointdlp'
            )
            
            foreach ($pattern in $protectionPatterns) {
                if ($headerString -match $pattern -or $headerUtf8 -match $pattern) {
                    return $true
                }
            }
            
            # PDF-specific protection markers
            if ($extension -eq '.pdf' -and $headerString -match '%PDF') {
                if ($headerString -match '/Encrypt\s|/O\s|/U\s|/P\s|Collection.*Protected') {
                    return $true
                }
            }
        }
        catch {
            # If we can't read the file, it might be protected
            return $true
        }
        
        return $false
    }
    catch {
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
Write-Log "Starting File Unprotect Script (Parallel Edition)"
Write-Log "Target Path: $Path"
Write-Log "Log File: $LogFile"
Write-Log "Parallel Threads: $ThrottleLimit"
Write-Log "Batch Size: $BatchSize"

# Check if target path exists
if (-not (Test-Path $Path)) {
    Write-Log "Target path not found: $Path" "ERROR"
    Write-Log "Please specify the correct path using -Path parameter"
    exit 1
}

# Initialize log file
Initialize-LogFile -LogPath $LogFile

# Define supported Microsoft Information Protection file extensions (based on official documentation)
# Source: https://learn.microsoft.com/en-us/information-protection/develop/concept-supported-filetypes
#
# Microsoft Information Protection supports two types of file protection:
# 1. Native Protection: File keeps original extension (.docx, .pdf, etc.) OR gets a new protected extension (.ptxt, .ppdf, etc.)
# 2. Generic Protection: File becomes .pfile regardless of original type
#
# Get supported extensions using function (centralized definition)
$supportedExtensions = Get-SupportedExtensions

# Use custom extensions if provided
if ($IncludeExtensions.Count -gt 0) {
    $supportedExtensions = $IncludeExtensions
    Write-Log "Using custom extensions: $($supportedExtensions -join ', ')"
}

# Get all files recursively with extension filtering and size filtering
Write-Log "Scanning for supported file types..."
Write-Log "Supported extensions: $($supportedExtensions -join ', ')"
Write-Log "Minimum file size: $MinFileSizeKB KB"
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

$allFiles = @()
$minSizeBytes = $MinFileSizeKB * 1024

# Use parallel file discovery for better performance with large datasets
$allFiles = $supportedExtensions | ForEach-Object -Parallel {
    $ext = $_
    $path = $using:Path
    $minSize = $using:minSizeBytes
    
    Get-ChildItem -Path $path -Recurse -File -Filter "*$ext" -Force -ErrorAction SilentlyContinue | 
        Where-Object { $_.Length -ge $minSize }
} -ThrottleLimit 10

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

# Performance optimization: Skip files that are clearly not protected
if ($QuickScan) {
    Write-Log "Quick scan mode enabled - excluding definitely protected extensions (handled by UltraFast script)"
    
    # Get already protected extensions using function (avoid duplication)
    $alreadyProtectedExtensions = Get-DefinitelyProtectedExtensions
    
    # In QuickScan mode, EXCLUDE already protected files (they should be handled by UltraFast script)
    $allFiles = $allFiles | Where-Object { 
        $ext = $_.Extension.ToLower()
        # Exclude definitely protected files AND require encrypted attribute for other files
        $ext -notin $alreadyProtectedExtensions -and (
            ($_.Attributes -band [System.IO.FileAttributes]::Encrypted) -or
            $ext -in @('.docx', '.xlsx', '.pptx', '.doc', '.xls', '.ppt', '.pdf', '.txt', '.jpg', '.png')
        )
    }
    Write-Log "Quick scan filtered to $($allFiles.Count) potentially protected files (excluded definitely protected extensions)"
}

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
        
        # Import the Test-FileProtected function (enhanced for parallel execution)
        function Test-FileProtected {
            param([string]$FilePath)
            
            try {
                $file = Get-Item $FilePath -Force -ErrorAction SilentlyContinue
                if ($file -and ($file.Attributes -band [System.IO.FileAttributes]::Encrypted)) {
                    return $true
                }
                
                # Quick check for already protected file extensions
                $extension = [System.IO.Path]::GetExtension($FilePath).ToLower()
                $protectedExtensions = @('.ptxt', '.pxml', '.pjpg', '.pjpeg', '.ppdf', '.ppng', 
                                        '.ptif', '.ptiff', '.pbmp', '.pgif', '.pjpe', '.pjfif', '.pfile')
                if ($extension -in $protectedExtensions) {
                    return $true
                }
                
                # ENHANCED: Check for alternate data streams (Purview protection indicator)
                try {
                    $streams = Get-Item $FilePath -Stream * -ErrorAction SilentlyContinue
                    if ($streams.Count -gt 1) {
                        $protectionStreams = $streams | Where-Object { 
                            $_.Stream -match 'sec\.endpointdlp|msip|rms|protection|irm' 
                        }
                        if ($protectionStreams.Count -gt 0) {
                            return $true
                        }
                    }
                }
                catch {
                    # Continue to other checks
                }
                
                # ENHANCED: Better header analysis
                try {
                    $header = [System.IO.File]::ReadAllBytes($FilePath) | Select-Object -First 2048
                    $headerString = [System.Text.Encoding]::ASCII.GetString($header)
                    $headerUtf8 = [System.Text.Encoding]::UTF8.GetString($header)
                    
                    $protectionPatterns = @(
                        'Microsoft\.Information\.Protection',
                        'MicrosoftIRMServices',
                        'Purview',
                        'RMS',
                        'Rights.*Management',
                        'MSIP',
                        'OfficeIRM',
                        'Protected\s+(PDF|Document)',
                        'DRMContent',
                        'sec\.endpointdlp'
                    )
                    
                    foreach ($pattern in $protectionPatterns) {
                        if ($headerString -match $pattern -or $headerUtf8 -match $pattern) {
                            return $true
                        }
                    }
                    
                    # PDF-specific check
                    if ($extension -eq '.pdf' -and $headerString -match '%PDF') {
                        if ($headerString -match '/Encrypt\s|/O\s|/U\s|/P\s|Collection.*Protected') {
                            return $true
                        }
                    }
                }
                catch {
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
    
    # Memory cleanup between batches (more aggressive for large datasets)
    if ($batchNumber % 5 -eq 0) {  # Every 5 batches
        [System.GC]::Collect()
        [System.GC]::WaitForPendingFinalizers()
        [System.GC]::Collect()
        Write-Log "Performed garbage collection (batch $batchNumber)"
    }
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