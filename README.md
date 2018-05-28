# AmiKo Windows (a.k.a. AmiKo C#)

AmiKo for Windows, written in C#.
Applications are available in the Microsoft store now!

| Name | Language |
|------|----------|
| [AmiKo Desitin] | Deutsch (Schweiz) |
| [CoMed Desitin] | Français (Suisse) |

[AmiKo Desitin]: https://www.microsoft.com/de-de/store/p/amiko-desitin/9wzdncrdffxc
[CoMed Desitin]: https://www.microsoft.com/de-de/store/p/comed-desitin/9nlldb9vxmgx

Another version for the macOS is also available. See [AmiKo-OSX](https://github.com/zdavatz/amiko-osx).


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

And for tests.

* NUnit (`>= 3.10.1`)
* NUnit Console (`>= 3.8.0`)

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

At first, you need to put database and csv files into `Data` directory.  
Once you have built the app with these files, you can update it via the feature
of the app.

```bash
$ cd /path/to/project
% mkdir -p AmiKoWindows/Data/{de,fr}

# AmiKoDesitin
$ cd AmiKoWindows/Data/de
$ curl -sLO http://pillbox.oddb.org/amiko_report_de.html

$ curl -sLO http://pillbox.oddb.org/amiko_db_full_idx_de.zip
$ curl -sLO http://pillbox.oddb.org/amiko_frequency_de.db.zip
$ curl -sLO http://pillbox.oddb.org/drug_interactions_csv_de.zip
$ unzip amiko_db_full_idx_de.zip
$ unzip amiko_frequency_de.db.zip
$ unzip drug_interactions_csv_de.zip

# CoMedDesitin
$ cd AmiKoWindows/Data/fr
$ curl -sLO http://pillbox.oddb.org/amiko_report_fr.html

$ curl -sLO http://pillbox.oddb.org/amiko_db_full_idx_fr.zip
$ curl -sLO http://pillbox.oddb.org/amiko_frequency_fr.db.zip
$ curl -sLO http://pillbox.oddb.org/drug_interactions_csv_fr.zip
$ unzip amiko_db_full_idx_fr.zip
$ unzip amiko_frequency_fr.db.zip
$ unzip drug_interactions_csv_fr.zip
```

#### Dependencies

##### Embedded

* Roboto (Apache-2.0)
* [ModernUIIcons](https://github.com/Templarian/WindowsIcons) (CC-BY-ND-3.0)
* [Glyphish](http://www.glyphish.com) (__NOT REDISTRIBUTED__ \*)

\*: We have purchased Glyphish icons and modified them for this project. It's
not redistributed under `GPL-3.0` (as icons). Because it's not open
source project, you cannot take them as icons from this project, can just use
them as a part of our source code. See [Glyphish-License.txt](
AmiKoWindows/Resources/Glyphish-License.txt).

##### NuGet

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

And then, you can download packages (into `Packages`).  
on PowerShell:

```powershell
# Downloads NuGet.exe (windows x86 Commandline) here
C:\Users\<USER>\AmiKo> .\NuGet.exe install "AmiKoWindows/Packages.config"
```

On Linux on Windows, it's not affected to long path name problem.  
on Bash (Linux on Windows with Mono):

```bash
# You can just do it (e.g. `/usr/local/bin/nuget.exe`)
user@host:/path/to/project $ nuget install AmiKoWindows/Packages.config
```

### Make

From this step, you may need to use PowerShell on Windows.

#### Using MSBuild

You would need to install *Microsoft Build Tools 2015* from [here](
https://www.microsoft.com/en-us/download/details.aspx?id=48159).

Use `>= 14.0` (installed one by *Microsoft Build Tools 2015*). Or, you may want
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
PS C:\Users\... > MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Clean
PS C:\Users\... > MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Build /p:Configuration=Debug

# CoMedDesitin
PS C:\Users\... > MSBuild.exe .\AmiKoWindows\CoMedDesitin.csproj /t:Clean
PS C:\Users\... > MSBuild.exe .\AmiKoWindows\CoMedDesitin.csproj /t:Build /p:Configuration=Debug
```

And then, you can start or kill `{AmiKo|CoMed}Desitin.exe` in `bin` directory like this:

```powershell
# AmiKoDesitin
PS C:\Users\... > Start-Process '.\AmiKoWindows\bin\Debug\AmiKo\AmiKo Desitin.exe'
PS C:\Users\... > Get-Process 'AmiKo Desitin' | Stop-Process
PS C:\Users\... > taskkill /im 'AmiKo Desitin.exe' /f

# CoMedDesitin
PS C:\Users\... > Start-Process '.\AmiKoWindows\bin\Debug\AmiKo\CoMed Desitin.exe'
PS C:\Users\... > Get-Process 'CoMed Desitin' | Stop-Process
PS C:\Users\... > taskkill /im 'CoMed Desitin.exe' /f
```

You need to debug the app with `DebugView` or `WinDbg` etc.

##### Reference

* [MSBuild](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild)
* [MSBuild Command Line Reference](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference)
* [DebugView](https://docs.microsoft.com/en-us/sysinternals/downloads/debugview)
* [WinDbg](https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools)


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
6. (Publish)
```

##### Reference

* [Compile and build in Visual Studio](https://docs.microsoft.com/en-us/visualstudio/ide/compiling-and-building-in-visual-studio)

### Release

#### Using MakeAppx

TODO

##### Reference

* [Create an app package with the MakeAppx.exe tool](https://docs.microsoft.com/en-us/windows/uwp/packaging/create-app-package-with-makeappx-tool)

#### Using Visual Studio

TODO

##### Reference

* [Package a UWP app with Visual Studio](https://docs.microsoft.com/en-us/windows/uwp/packaging/packaging-uwp-apps)


## Test

See projects in `AmiKoWindows.Tests`. Tests are written in NUnit.

```
PS C:\Users\... > taskkill /im 'MSBuild.exe' /f

# AmiKoDesitin
PS C:\Users\... > MSBuild.exe .\AmiKoWindows.Tests\AmiKoDesitin.Test.csproj /t:Clean
PS C:\Users\... > MSBuild.exe .\AmiKoWindows.Tests\AmiKoDesitin.Test.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU
PS C:\Users\... > .\Package\NUnit.ConseleRunner.3.8.0\tools\nunit3-console.exe .\AmiKoWindows.Tests\bin\Debug\AmiKo\AmiKoDesitin.Test.dll

# CoMedDesitin
PS C:\Users\... > MSBuild.exe .\AmiKoWindows.Tests\CoMedDesitin.Test.csproj /t:Clean
PS C:\Users\... > MSBuild.exe .\AmiKoWindows.Tests\CoMedDesitin.Test.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU
PS C:\Users\... > .\Package\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe .\AmiKoWindows.Tests\bin\Debug\CoMed\CoMedDesitin.Test.dll
```

##### Reference

* [NUnit](https://github.com/nunit/nunit)
* [NUnit Console](https://github.com/nunit/nunit-console)


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
