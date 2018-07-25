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

$lang = "de-CH"
if ($application -eq "CoMed") {
    $lang = "fr-CH"
}
###############################################################################

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

$version = "1.0.9.0"
$appId = "yweseeGmbH.${application}"
$appName = "${application} Desitin"
$description = "${application} Desitin"
$publisherId = "CN=3F71A827-F362-4FF2-A406-EA63C19EA85B"
$publisherName = "ywesee GmbH"
$arch = "x64"

$windowsKit = "10.0.17134.0"
#####

Write-Host
Write-Host ">> Build Info:"
Write-Host
Write-Host "------------------------------------------------------------------"
Write-Host "App Id: ${appId}"
Write-Host "App Name: ${appName}"
Write-Host "App Description: ${description}"
Write-Host "Version: ${version}"
Write-Host "Publisher Id: ${publisherId}"
Write-Host "Publisher Name: ${publisherName}"
Write-Host "Configuration: ${configuration}"
Write-Host "Arch: ${arch}"
Write-Host "Language: ${lang}"
Write-Host
Write-Host "Output Directory: ${outputDir}"
Write-Host "------------------------------------------------------------------"
Write-Host

###############################################################################

# Building

Write-Host
Write-Host ">> BUILD"
Write-Host

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
  Write-Host ">> ERROR"
  Write-Host ">> Build FAILED. Check build with `BuildAndRun.ps1`."
  exit 1
}

taskkill /im "${application} Desitin.exe" /f

###############################################################################

# Convert + Packaging

Write-Host
Write-Host ">> CONVERT"
Write-Host

Write-Host

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
  -Publisher "${publisherId}" `
  -PackageArch "$arch" `
  -PackagePublisherDisplayName "${publisherName}" `
  -Version "${version}" `
  -MakeAppx -sign -Verbose -Verify

if ($lastexitcode -ne 0) {
  Write-Host
  Write-Host ">> ERROR"
  Write-Host
  exit 0
}

if ($configuration -eq "Debug") {
  Write-Host
  Write-Host ">> DONE"
  Write-Host ">> \"Debug\" build has been detected. Trying to register the appx."
  Add-AppxPackage -Register "${outputDir}\${appId}\PackageFiles\AppxManifest.xml"
  exit 0
}
