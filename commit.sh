#!/usr/bin/env bash

set -euo pipefail

commit_message_input=${1-}

if [[ -z "${commit_message_input//[[:space:]]/}" ]]; then
  echo "No commit message was provided." >&2
  cat <<'EOF' >&2
Usage:
  ./commit-staged-files.sh "Your commit message here"

Notes:
  - Wrap the commit message in double quotes if it contains spaces.
  - The message is passed as the first positional parameter to the script.
EOF
  exit 1
fi

commit_message=$1

if ! git rev-parse --git-dir >/dev/null 2>&1; then
  echo "Not inside a git repository." >&2
  exit 1
fi

staged_files=()
while IFS= read -r file; do
  if [ -n "${file}" ]; then
    staged_files+=("${file}")
  fi
done < <(git diff --name-only --cached)

if [ "${#staged_files[@]}" -eq 0 ]; then
  echo "No staged files to commit."
  exit 0
fi

cat <<'EOF'
Staged files (commit order preserved):
EOF
printf '  %s\n' "${staged_files[@]}"
echo

for file in "${staged_files[@]}"; do
  echo "Committing ${file}..."
  git commit --only --message "${commit_message}" -- "${file}"
done

echo "Completed committing ${#staged_files[@]} file(s)."
