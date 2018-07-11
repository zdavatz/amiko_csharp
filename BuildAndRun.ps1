#!/usr/bin/env powershell -File

param(
    [string]$application,
    [string]$configuration
)

# NOTE:
#
# ```
# > powershell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "AmiKo"
# > powershell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "AmiKo" "Debug"
# > powershell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "CoMed"
# > powershell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "CoMed" "Release"
# ```

if ($application -ne "AmiKo" -and $application -ne "CoMed") {
  exit 1
}

if ($configuration -ne "Release") {
  $configuration = "Debug"
}

# AmiKo|CoMed
Write-Host "Application: ${application}"
Write-Host "Configuration: ${configuration}"


taskkill /im 'MSBuild.exe' /f
taskkill /im "{$application} Desitin.exe" /f

# Linux
wsl rm -f "AmiKoWindows/bin/${configuration}/$application/$application Desitin.exe"
wsl rm -f "AmiKoWindows/obj/${configuration}/$application Desitin.exe"

## Clean All (resources, db and cache etc.)
wsl rm -fr "AmiKoWindows/bin/${configuration}/$application/*exe*"
wsl rm -fr "AmiKoWindows/obj/**/${configuration}/*exe"

MSBuild.exe .\AmiKoWindows\"${application}"Desitin.csproj /t:Clean

# Build
MSBuild.exe .\AmiKoWindows\"${application}"Desitin.csproj /t:Build `
  /p:Configuration="${configuration}" `
  /p:Platform=x64 `
  /p:Log=Trace

if ($lastexitcode -ne 0) {
  Write-Host "Build faild with status: $lastexitcode"
  exit
}

# Run the application
Write-Host ""
Write-Host "Application '${application} Desitin.exe' is starting..." -NoNewLine
Start-Process ".\AmiKoWindows\bin\${configuration}\${application}\${application} Desitin.exe"
