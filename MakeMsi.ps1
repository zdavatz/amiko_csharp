#!/usr/bin/env powershell -File
$env:Path += ";C:\Program Files (x86)\WiX Toolset v3.11\bin"

New-Item -ItemType directory -Path AmiKoWindows\bin\Release\ARM64\Amiko-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\X64\Amiko-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\X86\Amiko-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\ARM64\Comed-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\X64\Comed-Installer
New-Item -ItemType directory -Path AmiKoWindows\bin\Release\X86\Comed-Installer

node wxs-generation.js "Amiko Desitin" ".\AmikoWindows\bin\Release\ARM64\AmiKo\net6.0-windows10.0.20348.0\win10-arm64\" "AmikoDesitin.wxs"
cat AmikoDesitin.wxs
candle AmikoDesitin.wxs

pushd AmiKoWindows\bin\Release\ARM64\AmiKo\net6.0-windows10.0.20348.0\win10-arm64

Write-Host "Creating MSI for Amiko ARM64"
light -ext WixUIExtension -cultures:de-DE ..\..\..\..\..\..\..\AmikoDesitin.wixobj -out ..\..\..\Amiko-Installer\Amiko-Installer-ARM64.msi
Write-Host "Created MSI for Amiko ARM64"

popd

node wxs-generation.js "Amiko Desitin" ".\AmikoWindows\bin\Release\X64\AmiKo\net6.0-windows10.0.20348.0\win10-x64\" "AmikoDesitin.wxs"

candle AmikoDesitin.wxs

pushd AmiKoWindows\bin\Release\X64\AmiKo\net6.0-windows10.0.20348.0\win10-x64

Write-Host "Creating MSI for Amiko X64"
light -ext WixUIExtension -cultures:de-DE ..\..\..\..\..\..\..\AmikoDesitin.wixobj -out ..\..\..\Amiko-Installer\Amiko-Installer-X64.msi
Write-Host "Created MSI for Amiko X64"

popd

node wxs-generation.js "Amiko Desitin" ".\AmikoWindows\bin\Release\X86\AmiKo\net6.0-windows10.0.20348.0\win10-x86\" "AmikoDesitin.wxs"
candle AmikoDesitin.wxs

pushd AmiKoWindows\bin\Release\X86\AmiKo\net6.0-windows10.0.20348.0\win10-x86

Write-Host "Creating MSI for Amiko X86"
light -ext WixUIExtension -cultures:de-DE ..\..\..\..\..\..\..\AmikoDesitin.wixobj -out ..\..\..\Amiko-Installer\Amiko-Installer-X86.msi
Write-Host "Created MSI for Amiko X86"

popd

node wxs-generation.js "Comed Desitin" ".\AmikoWindows\bin\Release\ARM64\CoMed\net6.0-windows10.0.20348.0\win10-arm64\" "AmikoDesitin.wxs"
candle AmikoDesitin.wxs

pushd AmiKoWindows\bin\Release\ARM64\CoMed\net6.0-windows10.0.20348.0\win10-arm64

Write-Host "Creating MSI for Comed ARM64"
light -ext WixUIExtension -cultures:fr-FR ..\..\..\..\..\..\..\AmikoDesitin.wixobj -out ..\..\..\Comed-Installer\Comed-Installer-ARM64.msi
Write-Host "Created MSI for Comed ARM64"

popd

node wxs-generation.js "Comed Desitin" ".\AmikoWindows\bin\Release\X64\CoMed\net6.0-windows10.0.20348.0\win10-x64\" "AmikoDesitin.wxs"
candle AmikoDesitin.wxs

pushd AmiKoWindows\bin\Release\X64\CoMed\net6.0-windows10.0.20348.0\win10-x64

Write-Host "Creating MSI for Comed X64"
light -ext WixUIExtension -cultures:fr-FR ..\..\..\..\..\..\..\AmikoDesitin.wixobj -out ..\..\..\Comed-Installer\Comed-Installer-X64.msi
Write-Host "Created MSI for Comed X64"

popd

node wxs-generation.js "Comed Desitin" ".\AmikoWindows\bin\Release\X86\CoMed\net6.0-windows10.0.20348.0\win10-x86\" "AmikoDesitin.wxs"
candle AmikoDesitin.wxs

pushd AmiKoWindows\bin\Release\X86\CoMed\net6.0-windows10.0.20348.0\win10-x86

Write-Host "Creating MSI for Comed X86"
light -ext WixUIExtension -cultures:fr-FR ..\..\..\..\..\..\..\AmikoDesitin.wixobj -out ..\..\..\Comed-Installer\Comed-Installer-X86.msi
Write-Host "Created MSI for Comed X86"

popd
