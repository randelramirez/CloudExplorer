# Windows publishing and installation

## Publishing

Publish on Windows 10/11 using [PowerShell 7 or later](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows) (`pwsh`) and the .NET 10 SDK. Windows PowerShell is not supported. Uno's self-contained Windows publishing currently needs to match the host architecture, which the script enforces.

For Windows x64:

```powershell
pwsh -File .\scripts\publish-windows.ps1 -Architecture x64
```

For Windows ARM64, run on an ARM64 Windows host:

```powershell
pwsh -File .\scripts\publish-windows.ps1 -Architecture arm64
```

The script writes a self-contained ZIP and SHA-256 file under the repository's root-level `installer\windows\` directory:

```text
cloud-explorer-<version>-win-<arch>.zip
cloud-explorer-<version>-win-<arch>.zip.sha256
```

It selects CoreCLR for the Uno Skia desktop target, disables trimming, removes debug symbols, verifies `CloudExplorer.exe`, and packages a `CloudExplorer\` directory. End users do not need a separate .NET installation.

### Authenticode signing

Install the Windows SDK so `signtool.exe` is available, import an appropriate code-signing certificate into the Windows certificate store, then publish with its thumbprint:

```powershell
pwsh -File .\scripts\publish-windows.ps1 `
  -Architecture x64 `
  -CertificateThumbprint "YOUR_CERTIFICATE_THUMBPRINT"
```

If SignTool is not on `PATH`, also pass `-SignToolPath`. The script signs and verifies the main executable before creating the ZIP. Certificate private keys are never accepted by or stored in the script.

Microsoft documents SignTool and timestamping at [SignTool](https://learn.microsoft.com/windows-hardware/drivers/devtest/signtool). Uno also supports ClickOnce when installer/update semantics are needed; see [Uno desktop publishing](https://platform.uno/docs/articles/uno-publishing-desktop.html#windows-clickonce).

## Installing

Place the ZIP and checksum file in the same directory and verify it:

```powershell
$package = ".\cloud-explorer-<version>-win-x64.zip"
$expected = (Get-Content "$package.sha256").Split(' ')[0]
$actual = (Get-FileHash -Algorithm SHA256 $package).Hash.ToLowerInvariant()
if ($actual -ne $expected) { throw "Cloud Explorer checksum mismatch." }
```

Extract it for the current user:

```powershell
$installRoot = Join-Path $env:LOCALAPPDATA "Programs"
New-Item -ItemType Directory -Force $installRoot | Out-Null
Expand-Archive $package -DestinationPath $installRoot -Force
& "$installRoot\CloudExplorer\CloudExplorer.exe"
```

Before upgrading, close Cloud Explorer and rename or remove `%LOCALAPPDATA%\Programs\CloudExplorer`; this prevents obsolete files from remaining after extraction.

To create a Start Menu shortcut:

```powershell
$shell = New-Object -ComObject WScript.Shell
$shortcutPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Cloud Explorer.lnk"
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = "$env:LOCALAPPDATA\Programs\CloudExplorer\CloudExplorer.exe"
$shortcut.WorkingDirectory = "$env:LOCALAPPDATA\Programs\CloudExplorer"
$shortcut.Save()
```

Install [AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html) and [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli-windows) using their official Windows installers. Open a new PowerShell window and confirm:

```powershell
aws --version
az version
```

An unsigned local build may trigger Microsoft Defender SmartScreen. For public distribution, Authenticode-sign the release instead of instructing users to bypass security warnings.

## Uninstalling

Close the application, then remove:

```powershell
Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Programs\CloudExplorer"
Remove-Item -Force "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Cloud Explorer.lnk" -ErrorAction SilentlyContinue
```
