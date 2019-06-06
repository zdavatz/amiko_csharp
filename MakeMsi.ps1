#!/usr/bin/env powershell -File
$env:Path += ";C:\Program Files (x86)\WiX Toolset v3.11\bin"

New-Item -ItemType directory -Path AmiKoWindows\bin\Release\Amiko-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\Comed-Installer

candle AmikoDesitin.wxs
candle ComedDesitin.wxs

cd AmiKoWindows\bin\Release\AmiKo

Write-Host "Creating MSI for Amiko"
light -ext WixUIExtension -cultures:de-DE ..\..\..\..\AmikoDesitin.wixobj -out ..\Amiko-Installer\Amiko-Installer.msi
Write-Host "Created MSI for Amiko"

cd ..\CoMed

Write-Host "Creating MSI for Comed"
light -ext WixUIExtension -cultures:fr-FR ..\..\..\..\ComedDesitin.wixobj -out ..\Comed-Installer\Comed-Installer.msi
Write-Host "Created MSI for Comed"
