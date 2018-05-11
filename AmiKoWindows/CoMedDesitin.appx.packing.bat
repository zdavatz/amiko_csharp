REM Make sure this file is encoded as UTF-8 (no BOM)

DEL "%1\bin\Executable\CoMedDesitin.appx"

ECHO --- Copying manifest file ---
COPY "%1\CoMedDesitin.appx.manifest" "%1\bin\Release\CoMed\Appx.manifest"

ECHO --- Running MakeAppx.exe ---
"C:\Program Files (x86)\Windows Kits\10\bin\x64\makeappx.exe" pack^
  /m %1\bin\Release\CoMed\Appx.manifest^
  /d %1\bin\Release\CoMed^
  /p %1\bin\Executable\CoMedDesitin.appx

:: "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe" sign^
:: /a /v /fd SHA256 %1\bin\Executable\CoMedDesitin.appx
PAUSE
