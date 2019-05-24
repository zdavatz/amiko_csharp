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

## Screenshots

<img src="/img/screenshot-1-20180713.png?raw=true" alt="Compendium" width="430px"> <img src="/img/screenshot-2-20180713.png?raw=true" alt="Interactions" width="430px"> <img src="/img/screenshot-3-20180713.png?raw=true" alt="Addressbook" width="430px"> <img src="/img/screenshot-4-20180713.png?raw=true" alt="Prescription" width="430px">


## Features

* All legally registered drugs of Switzerland
* `15'913` Drug-Drug Interactions
* Full-Text-Search (FTS) with Keyword highlighting
* Patient Management (outlook format CSV integration)
* Prescriptions (with comment, import/export support)
* Documents Printing

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

* NuGet `>= 4.6.2`
* MSBuild `>= 14.0`
* .NET Core SDK (`2.1.105`)
* .NET Core Runtime (`2.0.7`)
* .NET Framework (`4.6.2`)

Or just setup *Visual Studio* `>= 2015`

And for testing.

* NUnit (`>= 3.10.1`)
* NUnit Console (`>= 3.8.0`)

For release.

* Windows Kit 10 (`10.0.17134.0`)
* Desktop App Converter

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
C:\Users\<USER>\AmiKo> .\NuGet.exe install "AmiKoWindows/packages.config"
```

On Linux on Windows, it's not affected to long path name problem.
on Bash (Linux on Windows with Mono):

```bash
# You can just do it (e.g. `/usr/local/bin/nuget.exe`)
user@host:/path/to/project $ nuget install AmiKoWindows/packages.config
```

### Make

From this step, you may need to use PowerShell on Windows.

* Configuration (`Debug` or `Release`)
* Platform (`AnyCPU`, `x86` or `x64`.)
* Log (`Trace` or None)

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
PS C:\Users\...> MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Clean
PS C:\Users\...> MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Build /p:Configuration=Debug

# CoMedDesitin
PS C:\Users\...> MSBuild.exe .\AmiKoWindows\CoMedDesitin.csproj /t:Clean
PS C:\Users\...> MSBuild.exe .\AmiKoWindows\CoMedDesitin.csproj /t:Build /p:Configuration=Debug
```

And then, you can start or kill `{AmiKo|CoMed}Desitin.exe` in `bin` directory like this:

```powershell
# AmiKoDesitin
PS C:\Users\...> Start-Process '.\AmiKoWindows\bin\Debug\AmiKo\AmiKo Desitin.exe'
PS C:\Users\...> Get-Process 'AmiKo Desitin' | Stop-Process
PS C:\Users\...> taskkill /im 'AmiKo Desitin.exe' /f

# CoMedDesitin
PS C:\Users\...> Start-Process '.\AmiKoWindows\bin\Debug\AmiKo\CoMed Desitin.exe'
PS C:\Users\...> Get-Process 'CoMed Desitin' | Stop-Process
PS C:\Users\...> taskkill /im 'CoMed Desitin.exe' /f
```

There is also a script to build and invoke the application.

```powershell
# AmiKoDesitin (Debug is default)
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "AmiKo"

# CoMedDesitin
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "CoMed" "Debug"
```

Finally, You need to debug the app with `DebugView` or `WinDbg` etc. (Set `/p:Log=Trace` for Trace)

##### Reference

* [MSBuild](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild)
* [MSBuild Command Line Reference](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference)
* [DebugView](https://docs.microsoft.com/en-us/sysinternals/downloads/debugview)
* [WinDbg](https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools)


#### Building with Visual Studio Community Edition

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

#### Building with Visual Studio Community Edition via Commandline

or you can add this to your Path

```
C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\amd64
```

and this will work as well from your Source Directory

```
PS C:\Users\...> MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Clean
PS C:\Users\...> MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Build /p:Configuration=Debug
PS C:\Users\...> Start-Process '.\AmiKoWindows\bin\Debug\AmiKo\AmiKo Desitin.exe'

# or just do
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 "AmiKo"
```

#### Building Installer with Visual Studio

- Get [Installer Project for Visual Studio](https://marketplace.visualstudio.com/items?itemName=visualstudioclient.MicrosoftVisualStudio2017InstallerProjects)
- Build Amiko / Comed with Release configuration
- Build Amiko / Comed Installer
- The output should be at e.g. `Amiko Destin/Release/Amiko Destin.msi`

##### Reference

* [Compile and build in Visual Studio](https://docs.microsoft.com/en-us/visualstudio/ide/compiling-and-building-in-visual-studio)


### Release

You would need following steps.

0. Convert Exe binary to Appx using `MakeRelease.ps1`
1. Update AppxManifest.xml
2. Re-Package Assets using `Package.ps1`
3. Re-Signing

#### 0. Using Desktop App Converter (Desktop Bridge)

Download `Desktop App Converter` from Microsoft Store. And then use
`MakeRelease.ps1` script with your signing certificate and key. (In PowerShell run as Administrator)

Before making release build, check build configuration and version etc. in
following files.

* `AmiKoWindows/{AmiKoDesitin.appx.manifest,CoMedDesitin.appx.manifest}`
* `AmiKoWindows/Properties/AssemblyInfo.cs`
* `MakeRelease.ps1`

```powershell
# As Administrator

# AmiKoDesitin
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\MakeRelease.ps1 "AmiKo" "Debug"
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\MakeRelease.ps1 "AmiKo" "Release"

# CoMedDesitin
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\MakeRelease.ps1 "CoMed" "Debug"
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\MakeRelease.ps1 "CoMed" "Release"
```

**Appx** will be generated in
`AmiKoWindows/bin/{Debug,Release}/Output/{AmiKo,CoMed}`.

#### 1. Fix AppxManifest.xml

Currently, `-AppFileTypes` option of **DesktopAppConverter** does not work
expectedly for out configurations. Although fix `AppxManifest.xml` manually.

Path:

* `AmiKoWindows/bin/Release/Output/yweseeGmbH.AmiKo/PackageFiles/AppxManifest.xml`
* `AmiKoWindows/bin/Release/Output/yweseeGmbH.CoMedDesitin/PackageFiles/AppxManifest.xml`

```
# add missing entries `Extensions`

<Applications>
  <Application>
    ...

    <Extensions>
      <uap:Extension Category="windows.fileTypeAssociation">
        <uap:FileTypeAssociation Name="amk">
          <uap:Logo>Assets\Square44x44Logo.scale-100.png</uap:Logo>
          <uap:SupportedFileTypes>
            <uap:FileType>.amk</uap:FileType>
          </uap:SupportedFileTypes>
        </uap:FileTypeAssociation>
      </uap:Extension>
    </Extensions>
  </Application>
</Applications>
```


#### 2. Bundle Assets for Windows 10

`Package.ps1` script does also this step. If you want manually do it again. you can follow these instructions:

```powershell
# As Administrator

# AmiKoDesitin
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\Package.ps1 "AmiKo" "Debug"
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\Package.ps1 "AmiKo" "Release"

# CoMedDesitin
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\Package.ps1 "CoMed" "Debug"
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\Package.ps1 "CoMed" "Release"
```

Or,

0. Make sure `'C:\Program Files (x86)\Windows Kits\10\bin\10.0.17134.0\x64\{makepri,makeappx}.exe'` exist (Version `10.0.17134`, and set also **PATH**, as you need)
1. Go to `AmiKoWindows/bin/Release/Output/yweseeGmbH.AmiKo/PackageFiles`
2. Copy all Assets in `AmiKoWindows/Assets/` to `AmiKoWindows/bin/Release/Output/yweseeGmbH.AmiKo/PackageFiles/Assets/` (Overwrite)
3. Create pri files
4. Re-Package using `MakeAppx.exe`

```powershell
# e.g. AmiKoDesitin

# Change directory into **PackageFiles**
PS C:\Users\...> cd AmiKoWindows/bin/Release/Output/yweseeGmbH.AmiKo/PackageFiles

PS C:\Users\...> rm .\Assets -r -fo
PS C:\Users\...> cp ..\..\..\..\..\Assets .

# MakePri.exe
PS C:\Users\...> 'makepri.exe' createconfig /cf priconfig.xml /dq de-CH

# It seems that it needs absolute path...
PS C:\Users\...> 'makepri.exe' new \
  /pr C:\Users\<user>\path\to\amiko_csharp\AmiKoWindows\bin\Release\Output\yweseeGmbH.AmiKo\PackageFiles\ \
  /cf C:\Users\<user>\path\to\amiko_csharp\AmiKoWindows\bin\Release\Output\yweseeGmbH.AmiKo\PackageFiles\priconfig.xml

# MakeAppx.exe
PS C:\Users\...> 'makeappx.exe' pack /d .\ /p "AmiKo Desitin"

# Replace appx
PS C:\Users\...> Move-Item -Path "AmiKoWindows\bin\Release\Output\yweseeGmbH.AmiKo\PackageFiles\AmiKo Desitin.appx" `
      -Destination "AmiKoWindows\bin\Release\Output\yweseeGmbH.AmiKo\yweseeGmbH.AmiKo.appx" -Force
```

#### 3. Re-Signing

```powershell
# Sign (again)
PS C:\Users\...> signtool.exe sign /fd <HASH ALGORITHM> /a /f <PFX> /p <PASSWORD> <FILE>.appx
```

NOTE:
You need to install this `pfx` certificate into **Trusted People** on **Local
Machine** via Certificate Wizard.


##### Reference

* https://aka.ms/converter
* https://docs.microsoft.com/en-us/windows/uwp/launch-resume/handle-file-activation
* https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-prepare
* https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-run-desktop-app-converter
* https://docs.microsoft.com/en-us/windows/uwp/app-resources/makepri-exe-command-options
* https://docs.microsoft.com/en-us/windows/uwp/packaging/sign-app-package-using-signtool


### Clean

To clean built cache data or resources etc. (for debug, .exe)

```powershell
PS C:\Users\...> taskkill /im 'AmiKo Desitin.exe' /f
PS C:\Users\...> MSBuild.exe .\AmiKoWindows\AmiKoDesitin.csproj /t:Clean

PS C:\Users\...> taskkill /im 'CoMed Desitin.exe' /f
PS C:\Users\...> MSBuild.exe .\AmiKoWindows\CoMedDesitin.csproj /t:Clean
```

#### User Settings

```powershell
# Just delete these directories (or delete `user.config` in there)
PS C:\Users\...> rm 'C:\Users\<USER>\AppData\Local\ywesee\AmiKo Desitin.exe*' -f -fo
PS C:\Users\...> rm 'C:\Users\<USER>\AppData\Local\ywesee\CoMed Desitin.exe*' -f -fo
```

#### Application Resources

* Fachinfo Text DB
* Favorites
* Interaction Basket
* Doctor(Operator) Profile Photo

```powershell
# e.g. Profile Photo (for debug, .exe)
PS C:\Users\...> rm 'C:\Users\<USER>\AppData\Roaming\ywesee\AmiKo Desitin\*.png' -f -fo
PS C:\Users\...> rm 'C:\Users\<USER>\AppData\Roaming\ywesee\CoMed Desitin\*.png' -f -fo
```

#### Contacts and Prescriptions

To reset *AddressBook* entries and *Prescriptions* files (`.amk` files).

```powershell
# AmiKoDesitin (for debug, .exe)
PS C:\Users\...> taskkill /im 'AmiKo Desitin.exe' /f
PS C:\Users\...> rm 'C:\Users\<USER>\AppData\Local\Temp\amiko*' -r -fo
PS C:\Users\...> rm 'C:\Users\<USER>\AppData\Roaming\ywesee\Amiko Desitin\amk\*' -r -fo
PS C:\Users\...> rm .\AmiKoWindows\bin\Debug\AmiKo\* -r -fo

# CoMedDesitin (for debug, .exe)
PS C:\Users\...> taskkill /im 'CoMed Desitin.exe' /f
PS C:\Users\...> rm 'C:\Users\<USER>\AppData\Local\Temp\comed*' -r -fo
PS C:\Users\...> rm 'C:\Users\<USER>\AppData\Roaming\ywesee\CoMed Desitin\amk\*' -r -fo
PS C:\Users\...> rm .\AmiKoWindows\bin\Debug\CoMed\* -r -fo
```


## Test

See projects in `AmiKoWindows.Tests`. Tests are written in NUnit.

```powershell
PS C:\Users\...> taskkill /im 'MSBuild.exe' /f

# AmiKoDesitin
PS C:\Users\...> MSBuild.exe .\AmiKoWindows.Tests\AmiKoDesitin.Test.csproj /t:Clean
PS C:\Users\...> MSBuild.exe .\AmiKoWindows.Tests\AmiKoDesitin.Test.csproj /t:Build /p:Configuration=Debug /p:Platform=x64
PS C:\Users\...> .\Package\NUnit.ConseleRunner.3.8.0\tools\nunit3-console.exe .\AmiKoWindows.Tests\bin\Debug\AmiKo\AmiKoDesitin.Test.dll --output TestOutput.log

# CoMedDesitin
PS C:\Users\...> MSBuild.exe .\AmiKoWindows.Tests\CoMedDesitin.Test.csproj /t:Clean
PS C:\Users\...> MSBuild.exe .\AmiKoWindows.Tests\CoMedDesitin.Test.csproj /t:Build /p:Configuration=Debug /p:Platform=x64
PS C:\Users\...> .\Package\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe .\AmiKoWindows.Tests\bin\Debug\CoMed\CoMedDesitin.Test.dll --output TestOutput.log
```

Or you can just execute tests using `RunTest.ps1` like this.

```powershell
# AmiKoDesitin
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\RunTest.ps1 "AmiKo"

# CoMedDesitin
PS C:\Users\...> PowerShell.exe -ExecutionPolicy Bypass -File .\RunTest.ps1 "CoMed"
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
