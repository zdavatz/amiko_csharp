
del CoMedDesitin.appx
robocopy "assets" "..\..\bin\Release\CoMed\store\assets"
"C:\Program Files (x86)\Windows Kits\10\bin\x64\makeappx.exe" pack /d ..\..\bin\Release\CoMed /p ..\..\bin\Executable\CoMedDesitin.appx
REM "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe" sign /a /v /fd SHA256 ..\..\bin\Executable\CoMedDesitin.appx
pause
