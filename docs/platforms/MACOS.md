# macOS publishing and installation

## Publishing

Native macOS release artifacts must be built on macOS with the .NET 10 SDK and Apple command-line tools. The script chooses the current Mac's architecture unless `--arch` is supplied.

Create an unsigned/local `.app` bundle wrapped in a metadata-preserving ZIP:

```bash
./scripts/publish-macos.sh --format app
```

Explicit architecture examples:

```bash
./scripts/publish-macos.sh --arch arm64 --format app
./scripts/publish-macos.sh --arch x64 --format app
```

Artifacts and SHA-256 files are written under the repository's root-level `installer/macOS/` directory:

```text
cloud-explorer-<version>-osx-<arch>.zip
cloud-explorer-<version>-osx-<arch>.zip.sha256
```

Uno's macOS publisher always makes app bundles self-contained. The script uses Uno's native `PackageFormat=app` pipeline and `ditto` so resource forks and bundle metadata survive ZIP creation.

### Signed and notarized distribution

Public downloads should use an Apple Developer ID and notarization. List available identities:

```bash
security find-identity -v -p codesigning
```

Store notarization credentials once in the macOS keychain, following Apple's `notarytool` instructions. Then create a signed and notarized DMG:

```bash
./scripts/publish-macos.sh \
  --format dmg \
  --codesign-key "Developer ID Application: Your Name (TEAMID)" \
  --notary-profile "cloud-explorer-notary"
```

For a signed PKG, an installer identity is also required:

```bash
./scripts/publish-macos.sh \
  --format pkg \
  --codesign-key "Developer ID Application: Your Name (TEAMID)" \
  --package-signing-key "Developer ID Installer: Your Name (TEAMID)" \
  --notary-profile "cloud-explorer-notary"
```

See [Uno macOS publishing](https://platform.uno/docs/articles/uno-publishing-desktop-macos.html) and Apple's [notarization documentation](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution) before distributing outside your development machines.

## Installing an app ZIP

Verify and extract the package without losing macOS bundle metadata:

```bash
shasum -a 256 -c cloud-explorer-<version>-osx-arm64.zip.sha256
ditto -x -k cloud-explorer-<version>-osx-arm64.zip /tmp/cloud-explorer-install
```

Then drag `CloudExplorer.app` into `/Applications`, or install for the current user:

```bash
mkdir -p "$HOME/Applications"
ditto /tmp/cloud-explorer-install/CloudExplorer.app "$HOME/Applications/CloudExplorer.app"
open "$HOME/Applications/CloudExplorer.app"
```

For a signed DMG, open it and drag Cloud Explorer to Applications. For a signed PKG, open the package and follow the Installer prompts.

Unsigned local builds can be opened using Finder's **Control-click → Open** confirmation. Do not remove quarantine attributes from downloaded public builds; distribute a signed and notarized package instead.

Install [AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html) and [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli-macos) through their official macOS instructions and verify them in Terminal:

```bash
aws --version
az version
```

If a Finder-launched app cannot locate a CLI installed only through a shell-specific `PATH`, use the vendor's system-wide installer or ensure the executable is available in a standard location such as `/usr/local/bin` or `/opt/homebrew/bin`.

## Uninstalling

Quit Cloud Explorer and move `CloudExplorer.app` from `/Applications` or `$HOME/Applications` to Trash. Cloud Explorer does not install a daemon, background agent, or polling service.
