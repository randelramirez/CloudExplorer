# Publishing index

Cloud Explorer has host-specific release scripts because native packaging, code signing, and runtime verification must happen on the target operating system.

| Target | Release script | Publishing and installation guide |
|---|---|---|
| Ubuntu/Linux | `./scripts/publish-ubuntu.sh` | [Ubuntu](platforms/UBUNTU.md) |
| Windows | `.\scripts\publish-windows.ps1` | [Windows](platforms/WINDOWS.md) |
| macOS | `./scripts/publish-macos.sh` | [macOS](platforms/MACOS.md) |

On Linux or macOS, `./scripts/publish.sh` dispatches to the current operating system's script. Every baseline package is self-contained, so end users do not need to install .NET. Release artifacts and SHA-256 checksum files are separated into the easy-to-find root-level `installer/windows/`, `installer/macOS/`, and `installer/ubuntu/` directories. Generated packages are ignored by Git while the directory README files remain visible.

The scripts deliberately do not commit, push, upload, or publish to a store. They only build local artifacts. Store uploads, certificate access, signing identity selection, and release promotion remain explicit publisher actions.

## Common release sequence

1. Set `ApplicationDisplayVersion` and the final `ApplicationId` in `CloudExplorer/CloudExplorer.csproj`.
2. Run `dotnet test CloudExplorer.slnx --configuration Release`.
3. Run the release script on the target OS.
4. Verify the generated checksum and test installation on a clean machine or VM.
5. Sign and notarize as required before public distribution.

The scripts accept `--version`/`-Version` for a one-off version override without editing the project file.

## Why builds are host-specific

Uno and .NET can restore many foreign runtime identifiers, but that does not validate native launch behavior. Uno's macOS `.app`, `.dmg`, and `.pkg` pipeline uses Apple tooling; Windows Authenticode uses the Windows SDK; Linux Snap packaging uses Snapcraft/LXD. A successful cross-compile is therefore not treated as a distributable release.

References: [Uno desktop publishing](https://platform.uno/docs/articles/uno-publishing-desktop.html), [.NET application publishing](https://learn.microsoft.com/dotnet/core/deploying/), and [.NET runtime identifiers](https://learn.microsoft.com/dotnet/core/rid-catalog).
