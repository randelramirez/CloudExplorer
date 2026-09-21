# Product requirements

## Platforms and framework

- Uno Platform single-project application using Skia Desktop.
- Supported operating systems: Windows, macOS, and Ubuntu/Linux.
- AWS and Azure CLIs are the authentication and data-access boundary.

## Application shell

- When the main desktop window first activates, automatically maximize it within the host's available desktop work area.
- Keep the window in the native overlapped/windowed presenter so operating-system UI such as the Windows taskbar, macOS menu bar and Dock, and Linux panels and docks remains available.
- Treat a rejected or unsupported maximize request as a logged, non-fatal condition; do not provide an application full-screen toggle.
- Launch in the **Both** provider view, with AWS and Azure visible side by side in equal-width columns.
- Provide explicit **Both**, **AWS**, and **Azure** view choices.
- Fill the provider workspace with the selected provider in AWS-only or Azure-only view.
- Preserve each provider's account selection, filters, grouping, cached resources, status, and refresh state when the view choice changes.
- Treat provider view selection as presentation state only; changing it must not refresh cloud data.
- Provide a light/dark mode toggle.
- All text, foreground, background, surface, border, status, and accent colors must adapt to the selected theme.

## AWS

- Discover and display every AWS CLI profile.
- Allow the user to select a profile.
- Verify authentication with `aws sts get-caller-identity`.
- If authentication is unavailable, offer CLI-owned authentication.
- Query the selected profile's AWS Resource Explorer default view.
- Group the cached result by resource type or tag.
- Refresh only when requested by the user after the initial view load.

## Azure

- Discover enabled Azure subscriptions from Azure CLI.
- Verify that the cached session can acquire an access token.
- If authentication is unavailable, offer `az login`.
- List resources for the selected subscription.
- Group the cached result by resource group or resource type.
- Refresh only when requested by the user after the initial view load.

## Resource metadata

- Show name, provider type, resource group/service, location/region, tags, created date, and last-modified date.
- Missing dates must be visibly represented as `Not exposed`.
- Do not substitute an inventory observation time for last-modified time.

## Performance and safety

- No polling, recurring timer, scheduled refresh, or automatic retry loop.
- Ignore overlapping refresh requests while one is active.
- Keep AWS and Azure initialization, caches, busy states, timestamps, and refresh commands independent in every provider view.
- Run CLI processes asynchronously, read stdout/stderr concurrently, enforce timeouts, and terminate child process trees on cancellation/timeout.
- Pass arguments through `ProcessStartInfo.ArgumentList`; never construct a shell command from user-controlled values.
- Virtualize the resource list.
- Perform filtering and grouping locally without another provider call.
- Never store or log credentials or tokens.
- Never commit or push from automation.
