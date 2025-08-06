> [!warning]
> This script was not working when I tested. It detects the files and runs the unprotect command, BUT the files remain encrypted.
> In the end, I used Purview's right-click "Apply Sensitivity Label" option and chose "Non-Business". But this process is slow as it runs through each file one at a time (vs below which is supposed to run things in multiple threads)

# Purview File Unprotection Scripts

Optimized PowerShell scripts for unprotecting Microsoft Purview/Information Protection encrypted files at scale.

## Quick Start

For most users:
```powershell
# Two-pass approach (recommended for large datasets)
.\Unprotect-Purview-Files-TwoPass.ps1 -Path "C:\YourFolder"
```

For single-pass approach:
```powershell
# Main script with QuickScan for speed
.\Unprotect-Purview-Files.ps1 -Path "C:\YourFolder" -QuickScan
```

## Script Architecture (Simplified & Optimized)

All scripts now use a **function-based approach** to eliminate duplication:

- **`Unprotect-Purview-Files.ps1`**: Main script with centralized extension definitions
- **`Unprotect-Purview-Files-UltraFast.ps1`**: Ultra-fast script that imports functions from main script
- **`Unprotect-Purview-Files-TwoPass.ps1`**: Minimal orchestration script

### Key Benefits:
- **No Code Duplication**: Extension lists and logic are centralized
- **Automatic Consistency**: All scripts use the same extension definitions
- **Simplified Maintenance**: Update extensions in one place
- **Optimized Performance**: Each script focuses on its specific use case

## Performance Strategy

For maximum efficiency with large datasets (>100K files):

### Two-Pass Strategy (Recommended)
1. **Pass 1: Ultra-Fast** - Process files with definitely protected extensions (.ptxt, .pxml, etc.)
2. **Pass 2: Normal with QuickScan** - Process potentially protected files

This approach balances speed and completeness, ensuring no protected files are missed.

## Supported File Types

Based on official Microsoft Information Protection SDK documentation:

### Native Protection (extension preserved)
- **Office**: .docx, .xlsx, .pptx, .doc, .xls, .ppt, etc.
- **PDF**: May remain .pdf or become .ppdf

### Generic Protection (extension changes)
- **Text**: .txt → .ptxt
- **Images**: .jpg → .pjpg, .png → .ppng
- **XML**: .xml → .pxml
- **Other**: Various → .pfile

## Usage Examples

### Basic Usage
```powershell
# Process OneDrive folder
.\Unprotect-Purview-Files.ps1 -Path "$env:USERPROFILE\OneDrive"

# Process with custom log location
.\Unprotect-Purview-Files.ps1 -Path "C:\Data" -LogFile "C:\Logs\unprotect.csv"
```

### Advanced Usage
```powershell
# High-performance for large datasets
.\Unprotect-Purview-Files.ps1 -Path "C:\LargeDataset" -ThrottleLimit 50 -BatchSize 10000 -QuickScan

# Two-pass with custom settings
.\Unprotect-Purview-Files-TwoPass.ps1 -Path "C:\Data" -ThrottleLimit 30
```

## Parameters

### Main Script (`Unprotect-Purview-Files.ps1`)
- **`-Path`**: Folder to process (default: `$env:USERPROFILE\OneDrive\Temp`)
- **`-LogFile`**: CSV log file path
- **`-ThrottleLimit`**: Parallel jobs (default: 25)
- **`-BatchSize`**: Files per batch (default: 5000)
- **`-QuickScan`**: Skip deep protection checks for speed
- **`-IncludeExtensions`**: Process only specific extensions
- **`-MinFileSizeKB`**: Skip files smaller than specified size

### Ultra-Fast Script (`Unprotect-Purview-Files-UltraFast.ps1`)
- **`-OnlyExtensions`**: Extensions to process (defaults to definitely protected)
- **`-FastMode`**: Skip all detection, process all files

### Two-Pass Script (`Unprotect-Purview-Files-TwoPass.ps1`)
- Automatically uses optimal settings for each pass
- Combines results and provides summary

## Output

All scripts generate CSV log files with:
- Timestamp
- File path
- Processing status
- Error details (if any)

The two-pass script provides additional summary statistics.

## Requirements

- Windows PowerShell 5.1 or PowerShell 7+
- Microsoft Information Protection client
- Appropriate permissions on target files/folders

## Performance Tips

1. **Use Two-Pass Strategy** for datasets >100K files
2. **Enable QuickScan** to skip unnecessary deep inspection
3. **Increase ThrottleLimit** on powerful machines
4. **Use SSD storage** for source and destination
5. **Close other applications** during processing

## Troubleshooting

### Common Issues
- **Access Denied**: Run as administrator or check file permissions
- **Module Not Found**: Install Microsoft Information Protection client
- **Memory Issues**: Reduce BatchSize and ThrottleLimit

### Performance Issues
- Use QuickScan mode
- Increase ThrottleLimit gradually
- Process smaller folders in parallel

## License

Open source - feel free to modify and distribute.
