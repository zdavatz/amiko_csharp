#!/usr/bin/env powershell -File
$env:Path += ";C:\Program Files (x86)\WiX Toolset v3.11\bin"

New-Item -ItemType directory -Path AmiKoWindows\bin\Release\ARM64\Amiko-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\X64\Amiko-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\X86\Amiko-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\ARM64\Comed-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\X64\Comed-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\X86\Comed-Installer

candle AmikoDesitin.wxs
candle ComedDesitin.wxs

cd AmiKoWindows\bin\Release\ARM64\AmiKo\net5.0-windows10.0.20348.0

Write-Host "Creating MSI for Amiko ARM64"
light -ext WixUIExtension -cultures:de-DE ..\..\..\..\..\..\AmikoDesitin.wixobj -out ..\..\Amiko-Installer\Amiko-Installer-ARM64.msi
Write-Host "Created MSI for Amiko ARM64"

cd ..\..\..\X64\AmiKo\net5.0-windows10.0.20348.0

Write-Host "Creating MSI for Amiko X64"
light -ext WixUIExtension -cultures:de-DE ..\..\..\..\..\..\AmikoDesitin.wixobj -out ..\..\Amiko-Installer\Amiko-Installer-X64.msi
Write-Host "Created MSI for Amiko X64"

cd ..\..\..\X86\AmiKo\net5.0-windows10.0.20348.0

Write-Host "Creating MSI for Amiko X86"
light -ext WixUIExtension -cultures:de-DE ..\..\..\..\..\..\AmikoDesitin.wixobj -out ..\..\Amiko-Installer\Amiko-Installer-X86.msi
Write-Host "Created MSI for Amiko X86"

cd ..\..\..\ARM64\CoMed\net5.0-windows10.0.20348.0

Write-Host "Creating MSI for Comed ARM64"
light -ext WixUIExtension -cultures:fr-FR ..\..\..\..\..\..\ComedDesitin.wixobj -out ..\..\Comed-Installer\Comed-Installer-ARM64.msi
Write-Host "Created MSI for Comed ARM64"

cd ..\..\..\X64\CoMed\net5.0-windows10.0.20348.0

Write-Host "Creating MSI for Comed X64"
light -ext WixUIExtension -cultures:fr-FR ..\..\..\..\..\..\ComedDesitin.wixobj -out ..\..\Comed-Installer\Comed-Installer-X64.msi
Write-Host "Created MSI for Comed X64"

cd ..\..\..\X86\CoMed\net5.0-windows10.0.20348.0

Write-Host "Creating MSI for Comed X86"
light -ext WixUIExtension -cultures:fr-FR ..\..\..\..\..\..\ComedDesitin.wixobj -out ..\..\Comed-Installer\Comed-Installer-X86.msi
Write-Host "Created MSI for Comed X86"
