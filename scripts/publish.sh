#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

case "$(uname -s)" in
  Linux)
    exec "${script_dir}/publish-ubuntu.sh" "$@"
    ;;
  Darwin)
    exec "${script_dir}/publish-macos.sh" "$@"
    ;;
  MINGW*|MSYS*|CYGWIN*)
    printf '%s\n' 'Use PowerShell 7+: pwsh -File .\scripts\publish-windows.ps1' >&2
    exit 2
    ;;
  *)
    printf 'Unsupported publishing host: %s\n' "$(uname -s)" >&2
    exit 2
    ;;
esac
