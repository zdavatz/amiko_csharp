#!/usr/bin/env powershell -File

# AmiKo|CoMed
param([string]$application)
Write-Host $application

# NOTE:
#
# > powershell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "AmiKo"
# > powershell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "CoMed"
#

if ($application -ne "AmiKo" -and $application -ne "CoMed") {
  exit 1
}

taskkill /im 'MSBuild.exe' /f
taskkill /im "$application Desitin.exe" /f

# Linux
wsl rm -f "AmiKoWindows/bin/Debug/$application/$application Desitin.exe"
wsl rm -f "AmiKoWindows/obj/Debug/$application Desitin.exe"

## Clean All (resources, db and cache etc.)
wsl rm -fr "AmiKoWindows/bin/Debug/$application/*exe*"
wsl rm -fr "AmiKoWindows/obj/**/Debug/*exe"

MSBuild.exe .\AmiKoWindows\"$application"Desitin.csproj /t:Clean

# Build
MSBuild.exe .\AmiKoWindows\"$application"Desitin.csproj /t:Build `
  /p:Configuration=Debug `
  /p:Platform=x64 `
  /p:Log=Trace

if ($lastexitcode -ne 0) {
  Write-Host "Build faild with status: $lastexitcode"
  exit
}

# Run the application
Write-Host ""
Write-Host "Application '$application Desitin.exe' is starting..." -NoNewLine
Start-Process ".\AmiKoWindows\bin\Debug\$application\$application Desitin.exe"
