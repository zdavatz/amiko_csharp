REM Make sure this file is encoded as UTF-8 (no BOM)

SET resxfile="%1\AmiKoWindows\Properties\Resources.resx"
IF EXIST %resxfile% (
  ECHO --- Deleting resource file ---
  DEL %resxfile%
)

ECHO --- Copying resource file ---

:: copy {de|fr}.resx to resx
(COPY /Y "%1\AmiKoWindows\Properties\%2" "%1\AmiKoWindows\Properties\Resources.resx")
