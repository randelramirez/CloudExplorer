# Architecture

## Layers

```text
MainPage / ProviderPane
        │
MainViewModel
        ├── AwsExplorerViewModel
        └── AzureExplorerViewModel
                │
       provider service interfaces
        ├── IAwsCliService
        └── IAzureCliService
                │
          ICommandRunner
                │
        installed aws / az CLIs
```

The UI knows only view models. Provider view models own cached resources, authentication state, grouping, filtering, and explicit commands. Provider services translate CLI JSON into the common `CloudResource` model. `ProcessCommandRunner` is the only component allowed to create processes.

## Data flow

1. The first visible provider initializes once.
2. Its view model discovers local account choices and validates the CLI session.
3. If authenticated, it performs one resource request and caches the normalized records.
4. Search and group changes rebuild virtualized rows from the cache.
5. No cloud call occurs again until the user refreshes or explicitly changes account scope.

AWS and Azure use separate singleton view models, commands, busy states, caches, and timestamps. Consequently, comparison-mode refreshes remain independent.

## Provider strategy

AWS uses Resource Explorer because it provides a cross-service inventory through a single CLI operation. Results are limited by AWS Resource Explorer coverage, view permissions, configured region, and its 1,000-result search limit. The app reports setup and permission failures without silently substituting a partial tagging-only inventory.

Azure uses `az resource list --subscription ...`, avoiding any dependency on an optional Azure CLI extension. Subscription selection is passed per command; Cloud Explorer does not mutate the CLI's global selected subscription.

## Authentication

- AWS session verification: `aws sts get-caller-identity --profile ...`.
- AWS sign-in: `aws sso login` for SSO-configured profiles; otherwise `aws login`.
- Azure session verification: `az account get-access-token` with output restricted to expiry, followed by `az account show` for display identity.
- Azure sign-in: `az login`.

Browser/device interaction is owned by the CLI process. Output containing tokens is never requested, persisted, or logged.

## UI and theming

`ProviderPane` is a reusable view rendered from provider-specific data templates. A flat row sequence and `DataTemplateSelector` allow one virtualized `ListView` to display group headings and resources. Theme dictionaries define every authored color separately for light and dark mode.

The normal shell uses a `TabView` with AWS at index zero. Full-screen comparison replaces the tabs with a two-star-column grid and requests the desktop host's native full-screen presenter.
