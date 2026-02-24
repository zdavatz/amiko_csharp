# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AmiKo Windows is a Swiss pharmaceutical information desktop application (WPF/C#) for drug compendium search, drug-drug interaction checking, patient management, and prescription handling. It ships as two language variants built from the same codebase:

- **AmiKoDesitin** — German (de-CH)
- **CoMedDesitin** — French (fr-CH)

The variant is controlled via conditional compilation symbols (`AMIKO` or `COMED`).

## Build Commands

Requires MSBuild and .NET 6.0 SDK. Target framework: `net6.0-windows10.0.20348.0`.

```powershell
# Build (AmiKo German variant)
MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Build /p:Configuration=Debug

# Build (CoMed French variant)
MSBuild.exe .\AmiKoWindows\CoMedDesitin.csproj /t:Build /p:Configuration=Debug

# Build and run shortcut
PowerShell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "AmiKo"
PowerShell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "CoMed" "Debug"

# Clean
MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Clean
```

## Testing

Tests use **NUnit 3.x** and are in `AmiKoWindows.Tests/src/`.

```powershell
# Build and run tests (AmiKo)
PowerShell.exe -ExecutionPolicy Bypass -File .\RunTest.ps1 "AmiKo"

# Or manually
MSBuild.exe .\AmiKoWindows.Tests\AmiKoDesitin.Test.csproj /t:Build /p:Configuration=Debug /p:Platform=x64
nunit3-console.exe .\AmiKoWindows.Tests\bin\Debug\AmiKo\AmiKoDesitin.Test.dll
```

## Architecture

### Single Namespace

All source code lives under the `AmiKoWindows` namespace in `AmiKoWindows/Source/`. UI code-behind files are in `Source/Interface/`.

### Two Project Files, One Source Tree

`AmiKoDesitin.csproj` and `CoMedDesitin.csproj` compile the same source with different conditional symbols and resources. Language-specific data files live in `AmiKoWindows/Data/{de,fr}/`.

### MVVM-lite with WPF

- UI framework: WPF with **MahApps.Metro** (MetroWindow base class)
- Core classes implement `INotifyPropertyChanged` for data binding
- XAML views in `Source/Interface/`, resources/styles in `AmiKoWindows/Resources/`
- HTML content displayed via WebBrowser control (drug info, interactions)

### Data Layer (SQLite)

- **DatabaseHelper** — Low-level SQLite connection wrapper using `Microsoft.Data.Sqlite`
- **MainSqlDb** — Drug/medication database (`amiko_db_full_idx_{de,fr}.db`), search and filtering
- **PatientDb** — Patient records database, CRUD operations
- **FullTextDb** — Full-text search index database
- Database files must be downloaded before first build (see `download.ps1` or README)

### Key Domain Models

- **Article** — Drug/medication entry (title, ATC code, substances, interactions)
- **Contact** — Patient record
- **Account** — Healthcare provider/operator profile
- **Medication** — Prescription medication with EAN codes

### Application State

**UIState** manages view states (Compendium, Favorites, Interactions, Prescriptions, FullTextSearch) and query types (Title, Author, ATCCode, Ingredient, RegNr, Application, EANCode, Fulltext).

### Integrations

- **GoogleSyncManager** — Google Drive cloud sync for patient data
- **HINClient/** — Healthcare Information Network (HIN) OAuth/SAML authentication
- **SmartCard** — PCSC/ISO7816 healthcare smart card support
- Credentials files (`GoogleConstants.cs`, `HINClientCredentials.cs`) are not committed; copy from `.sample` files

### File Handling

- `.amk` file type association for prescription import/export (JSON-based)
- Single-instance enforcement via Mutex with file activation through WndProc messaging

## Release & Installers

- **WiX Toolset** MSI installers: `MakeMsi.ps1`
- **APPX** packages for Microsoft Store: `MakeRelease.ps1`, `Package.ps1`
- CI/CD via Azure Pipelines (`azure-pipelines.yml`)

## License

GPL-3.0, Copyright ywesee GmbH.
