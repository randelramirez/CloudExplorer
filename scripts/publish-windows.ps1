#requires -Version 7.0
#requires -PSEdition Core

[CmdletBinding()]
param(
    [ValidateSet("x64", "arm64")]
    [string]$Architecture = "x64",

    [string]$Version,

    [string]$CertificateThumbprint,

    [string]$SignToolPath,

    [uri]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDirectory
$projectFile = Join-Path $projectRoot "CloudExplorer\CloudExplorer.csproj"
$artifactsDirectory = Join-Path $projectRoot "artifacts"
$packagesDirectory = Join-Path (Join-Path $projectRoot "installer") "windows"
$stagingDirectory = Join-Path $artifactsDirectory (".publish-windows." + [guid]::NewGuid().ToString("N"))

if (-not $IsWindows) {
    throw "The Windows release package must be published on Windows."
}

$hostArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
if ($hostArchitecture -ne $Architecture) {
    throw "Self-contained Windows publishing must match the host architecture. Host: $hostArchitecture; requested: $Architecture."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK is required on PATH."
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = (& dotnet msbuild $projectFile -nologo -getProperty:ApplicationDisplayVersion | Select-Object -Last 1).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "Could not read ApplicationDisplayVersion."
    }
}

if ([string]::IsNullOrWhiteSpace($Version) -or $Version -notmatch '^[0-9A-Za-z][0-9A-Za-z.+-]*$') {
    throw "Invalid application version: $Version"
}

New-Item -ItemType Directory -Force -Path $packagesDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $stagingDirectory | Out-Null

try {
    $runtimeIdentifier = "win-$Architecture"
    $bundleDirectory = Join-Path $stagingDirectory "CloudExplorer"
    $archivePath = Join-Path $packagesDirectory "cloud-explorer-$Version-$runtimeIdentifier.zip"

    $publishArguments = @(
        "publish", $projectFile,
        "--framework", "net10.0-desktop",
        "--configuration", "Release",
        "--runtime", $runtimeIdentifier,
        "--self-contained", "true",
        "-p:TargetFrameworks=net10.0-desktop",
        "-p:UseMonoRuntime=false",
        "-p:PublishTrimmed=false",
        "-p:ApplicationDisplayVersion=$Version",
        "--output", $bundleDirectory
    )

    & dotnet @publishArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    $executablePath = Join-Path $bundleDirectory "CloudExplorer.exe"
    if (-not (Test-Path $executablePath -PathType Leaf)) {
        throw "Published executable was not found: $executablePath"
    }

    $iconPath = Join-Path $bundleDirectory "icon.ico"
    if (-not (Test-Path $iconPath -PathType Leaf) -or (Get-Item $iconPath).Length -eq 0) {
        throw "Published application icon was not found: $iconPath"
    }

    Get-ChildItem -Path $bundleDirectory -Filter *.pdb -Recurse | Remove-Item -Force

    if (-not [string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
        if ([string]::IsNullOrWhiteSpace($SignToolPath)) {
            $signToolCommand = Get-Command signtool.exe -ErrorAction SilentlyContinue
            if (-not $signToolCommand) {
                throw "signtool.exe was not found. Supply -SignToolPath or install the Windows SDK."
            }
            $SignToolPath = $signToolCommand.Source
        }

        & $SignToolPath sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampUrl.AbsoluteUri /td SHA256 $executablePath
        if ($LASTEXITCODE -ne 0) {
            throw "Authenticode signing failed with exit code $LASTEXITCODE."
        }
        & $SignToolPath verify /pa /v $executablePath
        if ($LASTEXITCODE -ne 0) {
            throw "Authenticode verification failed with exit code $LASTEXITCODE."
        }
    }

    if (Test-Path $archivePath) {
        Remove-Item -Force $archivePath
    }
    Compress-Archive -Path $bundleDirectory -DestinationPath $archivePath -CompressionLevel Optimal

    $checksum = (Get-FileHash -Algorithm SHA256 -Path $archivePath).Hash.ToLowerInvariant()
    $checksumPath = "$archivePath.sha256"
    Set-Content -NoNewline -Encoding ascii -Path $checksumPath -Value "$checksum  $(Split-Path -Leaf $archivePath)"

    Write-Output "Package: $archivePath"
    Write-Output "Checksum: $checksumPath"
}
finally {
    if ($stagingDirectory.StartsWith((Join-Path $artifactsDirectory ".publish-windows."), [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path $stagingDirectory)) {
        Remove-Item -Recurse -Force $stagingDirectory
    }
}
