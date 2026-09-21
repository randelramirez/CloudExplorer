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

1. `MainViewModel.SelectedProviderViewMode` starts as `ProviderViewMode.Both`; the other explicit values are `Aws` and `Azure`.
2. Each visible provider initializes at most once. The default Both view therefore initializes AWS and Azure independently.
3. Each provider view model discovers its local account choices and validates its own CLI session.
4. If authenticated, that provider performs one resource request and caches the normalized records.
5. Search and group changes rebuild virtualized rows from the provider's cache.
6. Changing provider view mode only changes presentation and preserves both provider view-model instances and their state.
7. No provider makes another cloud call until the user refreshes it or explicitly changes its account scope.

AWS and Azure use separate singleton view models, commands, busy states, caches, and timestamps. A refresh in the Both view targets only the provider whose refresh command the user invoked.

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

The shell offers explicit Both, AWS, and Azure provider-view choices. Both is the default and presents the two existing provider panes in equal-width columns. A single-provider choice gives that pane the full provider workspace. The shell reuses the same AWS and Azure view-model instances across modes, so view selection neither resets provider state nor couples refresh behavior.

On the main window's first `Activated` event, `App` detaches its one-time handler and makes a best-effort `OverlappedPresenter.Maximize()` request. Waiting for native activation lets the Linux window manager honor the request without adding a timer. Maximization asks each desktop host to fill its available work area while preserving operating-system UI such as taskbars, menu bars, panels, and docks. A missing overlapped presenter or rejected maximize request is logged as a warning and does not prevent startup. There is no application full-screen control or full-screen state in the view model; provider-view mode is independent of native window state.
