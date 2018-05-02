# AmiKo Windows (aka. AmiKo C#)

AmiKo for Windows, written in C#.
Applications are available in the Microsoft store now!

| Name | Language |
|------|----------|
| [AmiKo Desitin] | Deutsch (Schweiz) |
| [CoMed Desitin] | Français (Suisse) |

[AmiKo Desitin]: https://www.microsoft.com/de-de/store/p/amiko-desitin/9wzdncrdffxc
[CoMed Desitin]: https://www.microsoft.com/de-de/store/p/comed-desitin/9nlldb9vxmgx


## Features

* All legally registered drugs of Switzerland
* `15'913` Drug-Drug Interactions
* Full-Text-Search (FTS) with Keyword highlighting

Search by:

* Drug Tradename
* Active Agent
* Registration Holder
* ATC-Code
* Drug Indication
* Registration-Number
* Full-Text Search

All data can be updated on daily basis.


## Build

### Requirements

* Git
* NuGet
* MSBuild (or Visual Studio 2017)
* .NET Core SDK
* Windows 10 SDK

### Setup

At first, you need to put database and csv files into `dbs` directories.

```bash
$ cd dbs/de
$ curl -sLO http://pillbox.oddb.org/amiko_report_de.html
$ curl -sLO http://pillbox.oddb.org/amiko_db_full_idx_de.zip
$ curl -sLO http://pillbox.oddb.org/amiko_frequency_de.db.zip
$ curl -sLO http://pillbox.oddb.org/drug_interactions_csv_de.zip
$ unzip amiko_db_full_idx_de.zip
$ unzip amiko_frequency_de.db.zip
$ unzip drug_interactions_csv_de.zip

$ cd dbs/fr
$ curl -sLO http://pillbox.oddb.org/amiko_report_fr.html
$ curl -sLO http://pillbox.oddb.org/amiko_db_full_idx_fr.zip
$ curl -sLO http://pillbox.oddb.org/amiko_frequency_fr.db.zip
$ curl -sLO http://pillbox.oddb.org/drug_interactions_csv_fr.zip
$ unzip amiko_db_full_idx_fr.zip
$ unzip amiko_frequency_fr.db.zip
$ unzip drug_interactions_csv_fr.zip
```

#### Dependencies (NuGet)

If you checked out the project in Linux on Windows (e.g. Debian),
`NuGet` can't handle long path, correctly. So you need to set symbolic link
using `mklink` and environment variable on Commant Prompt (as Administrator).

See https://github.com/NuGet/Home/issues/3324

```powershell
# Environment variable `AmiKo` is set as:
# C:\Users\<USER>\AppData\Local\Packages\TheDebian...\LocalState\rootfs
#   \home\<user>\path\to\project

C:\Windows\system32> cd C:\Users\<USER>

# Or `cmd /c mklink /D AmiKo %AmiKo%` on PowerShell
C:\Users\<USER>> mklink /d AmiKo %AmiKo%
C:\Users\<USER>> cd AmiKo
C:\Users\<USER>\AmiKo>

: Downloads NuGet.exe (windows x86 Commandline) here
C:\Users\<USER>\AmiKo> NuGet.exe install "AmiKoWindows/packages.config" \
  -o packages/
```

### Make

#### MSBuild

You may want to use `Developer Command Prompt for VS 2017` (if you have it).

```powershell
> MSBuild.exe
```

##### Reference

* [MSBuild](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild)
* [MSBuild Command Line Reference](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference)


#### Visual Studio

```txt
1. AmiKoWindows -> Navigate {AmiKoDesitin|CoMedDesitin} -> Properties (Right Click)
  a. Set assembly name
  b. Set assembly information (Title, Product, Assembly version, File version)
2. Confirm Signing Tab
3. Check Security

4. Set target project using `Set as StartUp Project` (Right Click on the Solution Name)
5. Clean Solution (both projects)
5. Rebuild target project (AmiKoDesitin or CoMedDesitin)

6. Publish
```

##### Reference

* [Compile and build in Visual Studio](https://docs.microsoft.com/en-us/visualstudio/ide/compiling-and-building-in-visual-studio)


## License

`GPL-3.0`

```txt
AmiKo for Windows 10 Copyright (c) ywesee GmbH
```


## Questions

Please contact

```txt
zdavatz@ywesee.com
+41 43 540 05 50
```
