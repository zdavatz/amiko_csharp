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

$version = "1.0.7.0"
$appId = "yweseeGmbH.${application}"
$appName = "${application} Desitin"
$description = "${application} Desitin"
$publisherId = "3F71A827-F362-4FF2-A406-EA63C19EA85B"
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
  -Publisher "CN=${publisherId}" `
  -PackageArch "$arch" `
  -PackagePublisherDisplayName "${publisherName}" `
  -Version "${version}" `
  -MakeAppx -sign -Verbose -Verify

if ($configuration -eq "Debug") {
  Write-Host
  Write-Host ">> DONE"
  Write-Host ">> \"Debug\" build has been detected. Trying to register the appx."
  Add-AppxPackage -Register "${outputDir}\${appId}\PackageFiles\AppxManifest.xml"
  exit 0
}

###############################################################################

# Configuring Assets for Windows 10

Write-Host
Write-Host ">> PACKAGE"
Write-Host

$toolsDir = "C:\Program Files (x86)\Windows Kits\10\bin\${windowsKit}\x64"
$makePri = "${toolsDir}\makepri.exe"
$makeAppx = "${toolsDir}\makeappx.exe"

if (!(Test-Path -Path $makePri) -or !(Test-Path -Path $makeAppx)) {
    Write-Host
    Write-Host ">> ERROR"
    Write-Host ">> makepri.exe or makeappx.exe not found."
    Write-Host ">> makepri.exe: ${makePri}"
    Write-Host ">> makeappx.exe: ${makeAppx}"
    Write-Host
    Write-Host ">> Info: You would need Windows Kit 10 (${windowsKit})"
    exit 1
} else {
    Write-Host
    $packagesDir = "${outputDir}\${appId}\PackageFiles"

    cd "${packagesDir}"
    rm .\Assets -r -fo
    Copy-Item "${currentDir}\AmiKoWindows\Assets" . -recurse

    & "${makePri}" createconfig /cf priconfig.xml /dq $lang
    & "${makepri}" new /pr "${packagesDir}\" /cf "${packagesDir}\priconfig.xml"

    & "${makeappx}" pack /d "${packagesDir}" /p "${appName}"

    Move-Item -Path "${packagesDir}\${appName}.appx" `
      -Destination "${outputDir}\${appId}\${appId}.appx" -Force

    Write-Host
    Write-Host ">> DONE"
    Write-Host ">> Appx: ${outputDir}\${appId}\${appId}.appx"
}
