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
* NuGet `>= 4.6.2`
* MSBuild `>= 14.0`
* .NET Core SDK (`2.1.105`)
* .NET Core Runtime (`2.0.7`)
* .NET Framework (`4.6.1`)
* Windows 8.1 SDK (available from [archive](
  https://developer.microsoft.com/en-us/windows/downloads/sdk-archive))

Or just setup *Visual Studio* `>= 2015`

### Setup

#### Checkout the source code

If you check out the source code on Linux on Windows, it seems that it must be
**readonly** on Windows. You can still build it on there on Windows using
MSBuild or Visual Studio etc., but you cannot modify existing files on there
from Windows side.

See: [Do not change Linux files using Windows apps and tools](
https://blogs.msdn.microsoft.com/commandline/2016/11/17/do-not-change-linux-files-using-windows-apps-and-tools/).

##### Possible Locations

* Linux on Windows (Readonly on Windows)
* Windows (Readable/Editable also on Linux on Windows via `/mnt`)

#### Prepare Initial Database Files

At first, you need to put database and csv files into `dbs` directories.  
Once you have built the app with these files, you can update it via the feature
of the app.

```bash
$ cd /path/to/project

$ cd AmiKoWindows/dbs/de
$ curl -sLO http://pillbox.oddb.org/amiko_report_de.html
$ curl -sLO http://pillbox.oddb.org/amiko_db_full_idx_de.zip
$ curl -sLO http://pillbox.oddb.org/amiko_frequency_de.db.zip
$ curl -sLO http://pillbox.oddb.org/drug_interactions_csv_de.zip
$ unzip amiko_db_full_idx_de.zip
$ unzip amiko_frequency_de.db.zip
$ unzip drug_interactions_csv_de.zip

$ cd AmiKoWindows/dbs/fr
$ curl -sLO http://pillbox.oddb.org/amiko_report_fr.html
$ curl -sLO http://pillbox.oddb.org/amiko_db_full_idx_fr.zip
$ curl -sLO http://pillbox.oddb.org/amiko_frequency_fr.db.zip
$ curl -sLO http://pillbox.oddb.org/drug_interactions_csv_fr.zip
$ unzip amiko_db_full_idx_fr.zip
$ unzip amiko_frequency_fr.db.zip
$ unzip drug_interactions_csv_fr.zip
```

#### Install Dependencies (NuGet)

If you have checked out the project on Linux on Windows, `NuGet` can't handle
long path on the PowerShell on Windows, correctly. So you need to set symbolic
link using `mklink` and environment variable on Command Prompt or PowerShell
(using `/c`).

See: [NuGet and long file name support #3324](
https://github.com/NuGet/Home/issues/3324).

```txt
# This is project location, for example environment variable `AmiKo` is set as:
# C:\Users\<USER>\AppData\Local\Packages\TheDebian...\LocalState\rootfs
#   \home\<user>\path\to\project

C:\Windows\system32> cd C:\Users\<USER>

# Or `cmd /c mklink /D AmiKo %AmiKo%` on PowerShell
C:\Users\<USER>> mklink /d AmiKo %AmiKo%
C:\Users\<USER>> cd AmiKo
C:\Users\<USER>\AmiKo>
```

And then, you can download packages.  
on PowerShell:

```powershell
# Downloads nuget.exe (windows x86 Commandline) here
C:\Users\<USER>\AmiKo> .\nuget.exe install "AmiKoWindows/packages.config" \
  -o packages/
```

On Linux on Windows, it's not affected to long path name problem.  
on Bash (Linux on Windows with Mono):

```bash
# You can just do it (e.g. `/usr/local/bin/nuget.exe`)
user@host:/path/to/project $ nuget install AmiKoWindows/packages.config \
  -o packages
```

### Make

From this step, you may need to use PowerShell on Windows.

#### Using MSBuild

You would need to install *Microsoft Build Tools 2015* from [here](
https://www.microsoft.com/en-us/download/details.aspx?id=48159).

Use `>= 14.0` (installed one *Microsoft Build Tools 2015*). Or, you may want
to use special command prompt like `Developer Command Prompt for VS 2017`
bundled in Visual Studio.


```powershell
# Check the location of `MSBuild.exe`
PS C:\Users\... > Resolve-Path HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersion\* | Get-ItemProperty -Name MSBuildToolsPath

MSBuildToolsPath : C:\Program Files (x86)\MSBuild\14.0\bin\amd64\
PSPath           : Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\14.0
...

MSBuildToolsPath : C\:Windows\Microsoft.NET\Framework64\v4.0.30319\
PSPath           : Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0
...
```

Build with `MSBuild` on PowerShell (You need to set *PATH* for `MSBuild.exe`):

```powershell
# AmiKoDesitin
PS C:\Users\... > MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Build /p:Configuration=Debug

# CoMedDesitin
PS C:\Users\... > MSBuild.exe .\AmiKoWindows\CoMedDesitin.csproj /t:Build /p:Configuration=Debug
```

And then, you can run `{AmiKo|CoMed}Desitin.exe` in `app.publish` directory.

```powershell
PS C:\Users\... > .\bin\Debug\AmiKo\app.publish\AmiKo.Desitin.exe
PS C:\Users\... > .\bin\Debug\AmiKo\app.publish\CoMed.Desitin.exe
```

##### Reference

* [MSBuild](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild)
* [MSBuild Command Line Reference](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference)


#### Using Visual Studio

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
AmiKo for Windows
Copyright (c) ywesee GmbH
```


## Questions

Please contact:

```txt
zdavatz@ywesee.com
+41 43 540 05 50
```
