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

$appId = "yweseeGmbH.${application}"
$appName = "${application} Desitin"

$windowsKit = "10.0.17134.0"

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
