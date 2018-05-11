REM Make sure this file is encoded as UTF-8 (no BOM)

ECHO --- Copying important files and folder ---

(ROBOCOPY "%1\AmiKoWindows\Resources\css" "%2\Resources\css" /NP /NJH)
(ROBOCOPY "%1\AmiKoWindows\Resources\js" "%2\Resources\js" /NP /NJH)
(ROBOCOPY "%1\AmiKoWindows\Resources\img" "%2\Resources\img" /NP /NJH)

(ROBOCOPY "%1\AmiKoWindows\dbs\%3" "%2\dbs" /NP /NJH)

(COPY /Y "%1\AmiKoWindows\store\%3\appx.manifest" "%2\AppxManifest.xml")
