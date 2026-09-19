# Cloud Explorer agent rules

These rules apply to every automated phase and every coding-agent session in this repository.

## Non-negotiable constraints

- Never run `git commit` or `git push`. The repository owner performs both manually.
- Do not stage files or mutate Git history. In particular, do not run `git add`, `git tag`, `git merge`, `git rebase`, `git reset`, `git checkout`, `git switch`, `git branch`, or `git init`.
- Read-only inspection with `git status`, `git diff`, and `git log` is permitted.
- Never add background polling, recurring timers, scheduled cloud refreshes, or hidden periodic API calls.
- The initial provider view may load once. After that, only an explicit user action may request fresh cloud data.
- AWS and Azure refresh operations must remain independent in comparison mode.
- Never persist, print, or log access tokens, secret keys, passwords, or CLI credential files.
- Authentication must be delegated to the installed AWS CLI and Azure CLI.

## Engineering gates

- Preserve the Uno Platform single-project Skia Desktop targets for Windows, macOS, and Linux.
- Keep provider integrations behind interfaces and command execution behind `ICommandRunner`.
- Keep provider timestamps nullable. Never present an observation/index timestamp as a modified timestamp.
- Use theme resources for every UI color and verify both light and dark themes.
- Use a virtualized resource list and keep filtering/grouping local to the current cached result.
- Run `dotnet build CloudExplorer.slnx` and `dotnet test CloudExplorer.slnx` after material changes.
- Do not claim a phase is complete while its verification gate is failing.

Read `docs/REQUIREMENTS.md`, `docs/ARCHITECTURE.md`, and `docs/IMPLEMENTATION_PHASES.md` before changing behavior.
