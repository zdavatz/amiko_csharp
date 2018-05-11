REM Make sure this file is encoded as UTF-8 (no BOM)

ECHO --- Copying important files and folder ---

(ROBOCOPY "%1\AmiKoWindows\Resources\css" "%2\Resources\css" /NP /NJH)
(ROBOCOPY "%1\AmiKoWindows\Resources\js" "%2\Resources\js" /NP /NJH)
(ROBOCOPY "%1\AmiKoWindows\Resources\img" "%2\Resources\img" /NP /NJH)

(ROBOCOPY "%1\AmiKoWindows\Data\%3" "%2\Data" /NP /NJH)

SET appname="AmiKoDesitin"

if %3=="fr" (
  SET appname="CoMedDesitin"
)
(COPY /Y "%1\AmiKoWindows\%appname%.appx.manifest" "%2\AppxManifest.xml")
