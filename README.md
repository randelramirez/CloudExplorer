# Cloud Explorer

Cloud Explorer is a cross-platform Uno Platform desktop application for browsing AWS and Azure resources through the user's installed cloud CLIs. It targets Skia Desktop on Windows, macOS, and Linux.

> [!IMPORTANT]
> Cloud Explorer is currently in active development. Features, behavior, and packaging may change, and production use is not yet recommended.

## Current capabilities

- Best-effort startup maximization within the desktop work area, preserving the Windows taskbar, macOS menu bar and Dock, and Linux panels and docks; unsupported hosts are handled non-fatally.
- AWS and Azure appear side by side in the default **Both** view.
- Explicit **Both**, **AWS**, and **Azure** view choices preserve each provider's state; a single-provider view fills the provider workspace.
- AWS CLI profile discovery and profile selection.
- AWS CLI authentication through `aws sso login` or `aws login`.
- AWS Resource Explorer inventory grouped by resource type or tag.
- Azure CLI authentication through `az login`.
- Azure subscription discovery and selection.
- Azure resources grouped by resource group or resource type.
- Independent, explicit provider refresh buttons with no background polling or cross-provider refresh.
- Local filtering that never makes a cloud request.
- Created and modified timestamps when the provider exposes them, otherwise `Not exposed`.
- Light and dark themes whose colors are entirely theme-resource based.
- Equal-width AWS/Azure panes in the Both view.
- Virtualized result rendering and guarded non-overlapping operations.

## Prerequisites

- .NET 10 SDK
- AWS CLI v2 for AWS features
- Azure CLI for Azure features
- An AWS Resource Explorer index and default view in the selected profile's configured region
- A supported desktop environment: Windows, macOS, or Linux with X11/compatible Uno Skia host support

Cloud Explorer does not read or store credentials itself. Authentication and cached sessions remain owned by the cloud CLIs.

Resource refreshes are designed to use no directly billed API in the baseline implementation; see [docs/COSTS.md](docs/COSTS.md) for the precise caveats.

## Run

```bash
dotnet restore CloudExplorer.slnx
dotnet run --project CloudExplorer/CloudExplorer.csproj --framework net10.0-desktop
```

## Verify

```bash
dotnet build CloudExplorer.slnx
dotnet test CloudExplorer.slnx
```

## Refresh behavior

The default Both view initializes AWS and Azure independently, once each. After that, each provider's resource data remains cached until the user presses that provider's **Refresh** button. Switching among Both, AWS, and Azure preserves both caches and does not request fresh cloud data. Changing the selected profile or subscription is also an explicit user action and loads only that newly selected scope. There are no timers, schedulers, recurring cloud calls, or coupled AWS/Azure refreshes.

## Timestamp semantics

Cloud resource APIs do not guarantee universal creation or modification fields. The application displays a date only when it is returned by the provider and records its source. AWS Resource Explorer's `LastReportedAt` is retained as an observation timestamp and is deliberately not shown as “last modified.” See [docs/DATE_METADATA.md](docs/DATE_METADATA.md).

## Publishing

Platform publishing starts with:

```bash
# Ubuntu or macOS: dispatch to the current host
./scripts/publish.sh

# PowerShell 7+
pwsh -File .\scripts\publish-windows.ps1
```

Complete publisher prerequisites and end-user installation steps are separated by operating system in the [publishing index](docs/PUBLISHING.md): [Ubuntu](docs/platforms/UBUNTU.md), [Windows](docs/platforms/WINDOWS.md), and [macOS](docs/platforms/MACOS.md).
