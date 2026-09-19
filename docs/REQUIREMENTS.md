# Product requirements

## Platforms and framework

- Uno Platform single-project application using Skia Desktop.
- Supported operating systems: Windows, macOS, and Ubuntu/Linux.
- AWS and Azure CLIs are the authentication and data-access boundary.

## Application shell

- Show AWS and Azure as separate tabs.
- Select AWS by default.
- Provide a light/dark mode toggle.
- Provide full-screen comparison mode with exactly equal AWS and Azure columns.
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
- Run CLI processes asynchronously, read stdout/stderr concurrently, enforce timeouts, and terminate child process trees on cancellation/timeout.
- Pass arguments through `ProcessStartInfo.ArgumentList`; never construct a shell command from user-controlled values.
- Virtualize the resource list.
- Perform filtering and grouping locally without another provider call.
- Never store or log credentials or tokens.
- Never commit or push from automation.
