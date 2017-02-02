REM Make sure this file is encoded as UTF-8 (no BOM)

ECHO --- Copying important files and folder ---

(COPY /Y "%1\AmiKoWindows\Properties\%2" "%1\AmiKoWindows\Properties\Resources.resx")
