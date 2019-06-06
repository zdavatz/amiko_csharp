#!/usr/bin/env powershell -File

New-Item -ItemType directory -Path AmiKoWindows/Data/de
New-Item -ItemType directory -Path AmiKoWindows/Data/fr

# AmiKoDesitin
cd AmiKoWindows/Data/de

# Remove-Item amiko_db_full_idx_de.db
# Remove-Item amiko_frequency_de.db
# Remove-Item amiko_report_de.html
# Remove-Item drug_interactions_csv_de.csv

Invoke-WebRequest -Uri http://pillbox.oddb.org/amiko_report_de.html -OutFile amiko_report_de.html
Invoke-WebRequest -Uri http://pillbox.oddb.org/amiko_db_full_idx_de.zip -OutFile amiko_db_full_idx_de.zip
Invoke-WebRequest -Uri http://pillbox.oddb.org/amiko_frequency_de.db.zip -OutFile amiko_frequency_de.db.zip
Invoke-WebRequest -Uri http://pillbox.oddb.org/drug_interactions_csv_de.zip -OutFile drug_interactions_csv_de.zip

Expand-Archive -Force -Path amiko_db_full_idx_de.zip -DestinationPath .
Expand-Archive -Force -Path amiko_frequency_de.db.zip -DestinationPath .
Expand-Archive -Force -Path drug_interactions_csv_de.zip -DestinationPath .

Remove-Item amiko_db_full_idx_de.zip
Remove-Item amiko_frequency_de.db.zip
Remove-Item drug_interactions_csv_de.zip

# CoMedDesitin
cd ../fr

# Remove-Item amiko_db_full_idx_fr.db
# Remove-Item amiko_frequency_fr.db
# Remove-Item amiko_report_fr.html
# Remove-Item drug_interactions_csv_fr.csv

Invoke-WebRequest -Uri http://pillbox.oddb.org/amiko_report_fr.html -OutFile amiko_report_fr.html
Invoke-WebRequest -Uri http://pillbox.oddb.org/amiko_db_full_idx_fr.zip -OutFile amiko_db_full_idx_fr.zip
Invoke-WebRequest -Uri http://pillbox.oddb.org/amiko_frequency_fr.db.zip -OutFile amiko_frequency_fr.db.zip
Invoke-WebRequest -Uri http://pillbox.oddb.org/drug_interactions_csv_fr.zip -OutFile drug_interactions_csv_fr.zip

Expand-Archive -Force -Path amiko_db_full_idx_fr.zip -DestinationPath .
Expand-Archive -Force -Path amiko_frequency_fr.db.zip -DestinationPath .
Expand-Archive -Force -Path drug_interactions_csv_fr.zip -DestinationPath .

Remove-Item amiko_db_full_idx_fr.zip
Remove-Item amiko_frequency_fr.db.zip
Remove-Item drug_interactions_csv_fr.zip

cd ../../..
