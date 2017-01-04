REM Make sure this file is encoded as UTF-8 (no BOM)

ECHO --- Copying important files and folder ---

(COPY /Y "%1\AmiKoWindows\store\appx.manifest" "%2\AppxManifest.xml")
(ROBOCOPY "%1\AmiKoWindows\dbs" "%2\dbs" /NP /NJH)
(ROBOCOPY "%1\AmiKoWindows\css" "%2\css" /NP /NJH)
(ROBOCOPY "%1\AmiKoWindows\jscripts" "%2\jscripts" /NP /NJH)
(ROBOCOPY "%1\AmiKoWindows\images" "%2\images" /NP /NJH)
(ROBOCOPY "%1\AmiKoWindows\htmls" "%2\htmls" /NP /NJH) ^& IF %ERRORLEVEL% LEQ 1 exit 0