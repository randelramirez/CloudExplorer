# Ubuntu publishing and installation

## Publishing

Publish on Ubuntu with the .NET 10 SDK installed. The default output is a self-contained `tar.gz` archive:

```bash
./scripts/publish-ubuntu.sh --arch x64
```

For Ubuntu ARM64:

```bash
./scripts/publish-ubuntu.sh --arch arm64
```

The script writes these files under the repository's root-level `installer/ubuntu/` directory:

```text
cloud-explorer-<version>-linux-<arch>.tar.gz
cloud-explorer-<version>-linux-<arch>.tar.gz.sha256
```

It disables trimming, removes debug symbols from the staged release, verifies that the executable and generated launcher icon exist, and archives a `CloudExplorer/` directory. The archive includes the .NET runtime, the application icon, and a user-level desktop-integration helper; AWS CLI and Azure CLI remain separate prerequisites.

### Native Snap package

Uno can generate a classic-confinement Snap with desktop integration. Prepare Snapcraft according to Uno's prerequisites, including Snapcraft and LXD, then run:

```bash
./scripts/publish-ubuntu.sh --arch x64 --format snap
```

The script intentionally does not enable Snapcraft destructive mode. That option can modify its build environment and should only be considered on a disposable CI worker.

Uno's authoritative setup and current limitations are documented in [Publishing Your App for Linux](https://platform.uno/docs/articles/uno-publishing-desktop.linux.html). The Uno SDK version pinned by this project recognizes Ubuntu 20.04, 22.04, and 24.04 when choosing a Snap base. On newer Ubuntu releases—including 26.04—use the portable archive or build the Snap on a supported Ubuntu release.

## Installing the portable archive

First verify the download from the directory containing both files:

```bash
sha256sum --check cloud-explorer-<version>-linux-x64.tar.gz.sha256
```

Install for the current user:

```bash
mkdir -p "$HOME/.local/opt" "$HOME/.local/bin"
tar -xzf cloud-explorer-<version>-linux-x64.tar.gz -C "$HOME/.local/opt"
ln -sfn "$HOME/.local/opt/CloudExplorer/CloudExplorer" "$HOME/.local/bin/cloud-explorer"
"$HOME/.local/opt/CloudExplorer/install-desktop-entry.sh"
"$HOME/.local/bin/cloud-explorer"
```

The desktop-integration helper installs the generated Cloud Explorer icon and a matching `.desktop` entry under the current user's XDG data directory. This makes the published portable app discoverable in the Ubuntu Dash and lets the running window group under the correct launcher icon. Keep the extracted `CloudExplorer/` directory at the same location after running the helper; rerun it if the directory is moved.

If upgrading an existing installation, rename or remove the existing `$HOME/.local/opt/CloudExplorer` directory before extracting so obsolete files cannot remain alongside the new release.

The self-contained .NET publish still relies on native desktop libraries. On supported Ubuntu versions, ensure the X11/graphics dependencies are installed if the app does not launch:

```bash
sudo apt update
sudo apt install dbus libfontconfig1 libgl1 libx11-6 libxi6 libxrandr2
```

Install [AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html) and [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli-linux) through their official instructions and confirm that both commands are on `PATH`:

```bash
aws --version
az version
```

## Installing the Snap

For a locally built, unsigned Snap:

```bash
sudo snap install ./CloudExplorer_<version>_amd64.snap --dangerous --classic
```

`--dangerous` acknowledges that the local package did not come from the Snap Store; it does not change confinement. `--classic` matches Uno's generated classic-confinement manifest. Store-distributed packages should be installed normally without `--dangerous`.

## Uninstalling

Portable installation:

```bash
rm "$HOME/.local/bin/cloud-explorer"
"$HOME/.local/opt/CloudExplorer/install-desktop-entry.sh" --uninstall
rm -rf "$HOME/.local/opt/CloudExplorer"
```

Snap installation:

```bash
sudo snap remove cloudexplorer
```

Confirm the exact Snap name with `snap list` because the generated name can follow the project packaging metadata.
