var Path = require('path');
var FS = require('fs');
var Crypto = require('crypto');

var appName = process.argv[2]; // "Amiko Desitin" / "Comed Desitin"
var appFolder = process.argv[3];
var outputPath = process.argv[4];

function directoryTag(absPath) {
    var isRoot = absPath === appFolder;
    var lastDirName = Path.basename(absPath);
    var relativePath = Path.relative(appFolder, absPath);
    var dirId = relativePath.replaceAll(Path.sep, '_').replaceAll('-', '_').replaceAll(' ', '_');
    var entries = FS.readdirSync(absPath, {
        withFileTypes: true,
    });
    var files = entries.filter(e => e.isFile());
    var directories = entries.filter(e => e.isDirectory());
    var xml = isRoot ? `<Directory Id='INSTALLDIR' Name='AmikoDesitin'>` : `<Directory Id='${dirId}' Name='${lastDirName}'>`;
    if (files.length) {
        var componentId = isRoot ? 'MainExecutable' : dirId + '_dir';
        xml += `<Component Id='${componentId}' Guid='${Crypto.randomUUID()}'>`;
        var markedKey = false;
        for (var fileEntry of files) {
            if (fileEntry.name === 'AmiKo Desitin.exe' || fileEntry.name === 'CoMed Desitin.exe') {
                var nameWithoutExe = Path.basename(fileEntry.name, Path.extname(fileEntry.name));
                // The exe is always at root and we force it to be the keyPath
                xml += `<File Id='AmikoEXE' Name='${fileEntry.name}' DiskId='1' Source='${fileEntry.name}' KeyPath='yes'>
                            <Shortcut Id="startmenuAmikoDesitin" Directory="ProgramMenuFolder" Name="${nameWithoutExe}"
                                WorkingDirectory='INSTALLDIR' Icon="desitin_icon.ico" IconIndex="0" Advertise="yes" />
                            <Shortcut Id="desktopAmikoDesitin" Directory="DesktopFolder" Name="${nameWithoutExe}"
                                WorkingDirectory='INSTALLDIR' Icon="desitin_icon.ico" IconIndex="0" Advertise="yes" />
                        </File>`;
                continue;
            }
            var fileId = dirId + '_' + fileEntry.name.replaceAll('-', '_').replaceAll(' ', '_');
            xml += `<File Name='${fileEntry.name}' Id='${fileId}' DiskId='1' Source='${Path.join(relativePath,fileEntry.name)}' KeyPath='${(isRoot || markedKey) ? 'no' : 'yes'}' />`;
            markedKey = true;
        }
        xml += `</Component>`;
    }
    for (var directoryEntry of directories) {
        var child = Path.join(absPath, directoryEntry.name);
        xml += directoryTag(child);
    }
    xml += `</Directory>`;
    return xml;
}

function componentRefTags(absPath) {
    var isRoot = absPath === appFolder;
    var relativePath = Path.relative(appFolder, absPath);
    var dirId = relativePath.replaceAll(Path.sep, '_').replaceAll('-', '_').replaceAll(' ', '_');
    var entries = FS.readdirSync(absPath, {
        withFileTypes: true,
    });
    var files = entries.filter(e => e.isFile());
    var directories = entries.filter(e => e.isDirectory());
    var xml = '';
    if (files.length) {
        var componentId = isRoot ? 'MainExecutable' : dirId + '_dir';
        xml += `<ComponentRef Id='${componentId}' />`;
    }
    for (var directoryEntry of directories) {
        var child = Path.join(absPath, directoryEntry.name);
        xml += componentRefTags(child);
    }
    return xml;
}

var template = `<?xml version='1.0' encoding='windows-1252'?>
<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>
    <Product Name='${appName}' Manufacturer='ywesee'
        Id='27CF26CA-B7AD-4B17-8929-A8B59F460AC4'
        UpgradeCode='F199632E-60C8-428A-8AE7-124562CC0735'
        Language='1031' Codepage='1252' Version='1.0.0'>

        <Package Id='*' Keywords='Installer' Description="${appName} Installer"
            Comments='Install ${appName}' Manufacturer='ywesee'
            InstallerVersion='100' Languages='1031' Compressed='yes' SummaryCodepage='1252' />

        <Media Id='1' Cabinet='Sample.cab' EmbedCab='yes' DiskPrompt='CD-ROM #1' />
        <Property Id='DiskPrompt' Value="${appName} Installation [1]" />

        <Directory Id='TARGETDIR' Name='SourceDir'>
            <Directory Id='ProgramFilesFolder' Name='PFiles'>
                <Directory Id='ywesee' Name='ywesee'>
                    ${directoryTag(appFolder)}
                </Directory>
            </Directory>
            <Directory Id="ProgramMenuFolder" Name="Programs" />
            <Directory Id="DesktopFolder" Name="Desktop" />
        </Directory>

        <Feature Id='Complete' Title='${appName}' Description='The complete package.' Display='expand' Level='1' ConfigurableDirectory='INSTALLDIR'>
            ${componentRefTags(appFolder)}
        </Feature>

        <Icon Id="desitin_icon.ico" SourceFile="Resources\\img\\desitin_icon.ico"/>

        <UI>
            <UIRef Id="WixUI_InstallDir" />
            <Publish Dialog="WelcomeDlg"
                     Control="Next"
                     Event="NewDialog"
                     Value="InstallDirDlg"
                     Order="2">1</Publish>
            <Publish Dialog="InstallDirDlg"
                     Control="Back"
                     Event="NewDialog"
                     Value="WelcomeDlg"
                     Order="2">1</Publish>
        </UI>
        <Property Id="WIXUI_INSTALLDIR" Value="INSTALLDIR" />
    </Product>
</Wix>
`;

FS.writeFileSync(outputPath, template);
