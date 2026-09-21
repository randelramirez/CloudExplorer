# Installer output

Published Cloud Explorer packages and their SHA-256 checksum files are separated by operating system:

- [`windows/`](windows/) for Windows ZIP packages
- [`macOS/`](macOS/) for macOS app ZIP, DMG, or PKG packages
- [`ubuntu/`](ubuntu/) for Ubuntu portable archives or Snap packages

Generate a package on the target operating system:

```bash
# Ubuntu or macOS
./scripts/publish.sh
```

```powershell
# PowerShell 7+
pwsh -File .\scripts\publish-windows.ps1
```

Generated binaries and checksums are intentionally ignored by Git. See the [publishing index](../docs/PUBLISHING.md) for signing, packaging, verification, and installation instructions for each operating system.
