#!/usr/bin/env powershell -File

param(
    [string]$application,
    [string]$configuration
)
Write-Host "Application: ${application}"
Write-Host "Configuration: ${configuration}"

if ($application -ne "AmiKo" -and $application -ne "CoMed") {
    exit 1
}

if ($configuration -ne "Debug" -and $configuration -ne "Release") {
    exit 1
}

$currentDir = (Get-Item ".\").FullName
$packageDir = "${currentDir}\AmiKoWindows\bin\${configuration}\${application}"
$outputDir = "${currentDir}\AmiKoWindows\bin\${configuration}\Output"


#########################
# Package Configuration #
#########################

# NOTE:
#
# See also:
#
# AmiKoWindows/AmiKoDesitin.appx.manifest
# AmiKoWindows/CoMedDesitin.appx.manifest
#
# References for Desktop Bridge and DesktopAppConverter.exe
#
# https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-manual-conversion
# https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-run-desktop-app-converter

$version = "1.0.7.0"
$appId = "yweseeGmbH.${application}"
$appName = "${application} Desitin"
$description = "${application} Desitin"
$publisherId = "3F71A827-F362-4FF2-A406-EA63C19EA85B"
$publisherName = "ywesee GmbH"
$arch = "x64"
#####


Write-Host "Output: ${outputDir}"

if (!(Test-Path -Path $outputDir)) {
    New-Item -ItemType directory -Path $outputDir
}

$objDir = "${currentDir}\AmiKoWindows\obj\${arch}\${configuration}"
if (!(Test-Path -Path $objDir)) {
    New-Item -ItemType directory -Path $objDir
}
rm "${objDir}" -r -fo

PowerShell.exe -ExecutionPolicy Bypass `
  -File "${currentDir}\BuildAndRun.ps1" "${application}" "${configuration}"

Write-Host $lastexitcode
if ($lastexitcode -ne 0) {
  Write-Host
  Write-Host "Build FAILED."
  exit 1
}

taskkill /im "${application} Desitin.exe" /f

Write-Host

# fix appId in microsoft storeh
if ($application -eq "CoMed") {
  $appId = "${appId}Desitin"
}

if ($configuration -eq "Debug") {
  $appId = "${appId}.Debug"
}

DesktopAppConverter.exe `
  -Installer "${packageDir}" `
  -AppExecutable "${application} Desitin.exe" `
  -AppDisplayName "${application} Desitin" `
  -AppDescription "${description}" `
  -AppId "${appId}" `
  -PackageName "${appId}" `
  -PackageDisplayName "${appName}" `
  -Destination "${outputDir}" `
  -Publisher "CN=${publisherId}" `
  -PackageArch "$arch" `
  -PackagePublisherDisplayName "${publisherName}" `
  -Version "${version}" `
  -MakeAppx -sign -Verbose -Verify

if ($configuration -eq "Debug") {
  Add-AppxPackage -Register "${outputDir}\${appId}\PackageFiles\AppxManifest.xml"
}
