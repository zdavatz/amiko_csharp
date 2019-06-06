#!/usr/bin/env powershell -File
$env:Path += ";C:\Program Files (x86)\WiX Toolset v3.11\bin"

candle AmikoDesitin.wxs
cd AmiKoWindows\bin\Release\AmiKo
light -ext WixUIExtension  ..\..\..\..\AmikoDesitin.wixobj
