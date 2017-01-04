
del AmiKoDesitin.appx
robocopy "assets" "..\bin\Release\store\assets"
"C:\Program Files (x86)\Windows Kits\10\bin\x64\makeappx.exe" pack /d ..\bin\Release /p ..\bin\Executable\AmiKoDesitin.appx
REM "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe" sign /a /v /fd SHA256 ..\bin\Executable\AmiKoDesitin.appx
pause
