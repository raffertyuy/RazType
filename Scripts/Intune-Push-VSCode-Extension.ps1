param($Extension)

Start-Transcript -Path "$ENV:windir\logs\VSIXInstaller_$Extension.log" | Out-Null

$Script:VSInstallationPath = "C:\Program Files (x86)\Microsoft Visual Studio"

Function Install-VSIXExtension([String] $PackageName) {
  $ErrorActionPreference = "Stop" $BaseProtocol = "https:" $BaseHostName = "marketplace.visualstudio.com"

  $Uri = "$($baseProtocol)//$($baseHostName)/items?itemName=$($PackageName)"
  $VsixLocation = "$($env:Temp)\$([guid]::NewGuid()).vsix"

  $VSInstallDir = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\resources\app\ServiceHub\Services\Microsoft.VisualStudio.Setup.Service"

  if (-Not $VSInstallDir) {
    Write-Error "Visual Studio InstallDir registry key missing"
    Exit 1
  }

  Write-Host "Grabbing VSIX extension at $($Uri)"
  $HTML = Invoke-WebRequest -Uri $Uri -UseBasicParsing -SessionVariable session

  Write-Host "Attempting to download $($PackageName)..."
  $anchor = $HTML.Links |
  Where-Object { $_.class -eq 'install-button-container' } |
  Select-Object -ExpandProperty href

  if (-Not $anchor) {
    Write-Error "Could not find download anchor tag on the Visual Studio Extensions page"
    Exit 1
  }
  Write-Host "Anchor is $($anchor)"
  $href = "$($baseProtocol)//$($baseHostName)$($anchor)"
  Write-Host "Href is $($href)"
  Invoke-WebRequest $href -OutFile $VsixLocation -WebSession $session

  if (-Not (Test-Path $VsixLocation)) {
    Write-Error "Downloaded VSIX file could not be located"
    Exit 1
  }
  Write-Host "VSInstallDir is $($VSInstallDir)"
  Write-Host "VsixLocation is $($VsixLocation)"
  Write-Host "Installing $($PackageName)..."
  Start-Process -Filepath "$($VSInstallDir)\VSIXInstaller" -ArgumentList "/q /a $($VsixLocation)" -Wait

  Write-Host "Cleanup..."
  rm $VsixLocation

  Write-Host "Installation of $($PackageName) complete!"
  New-item -Path $VSInstallationPath -ItemType File -Name $Extension -Value "Installed" -Force

}


if((Get-Item -Path $VSInstallationPath).Exists) { Install-VSIXExtension -PackageName $Extension } else { Exit(1) }

Stop-Transcript | Out-Null