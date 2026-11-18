#!/usr/bin/env bash
set -euo pipefail
trap 'rc=$?; if [ $rc -ne 0 ] && [ $rc -ne 2 ]; then echo "guard.sh failed (exit $rc): blocking by default" >&2; exit 2; fi' EXIT
command -v jq >/dev/null || { echo "guard.sh needs jq" >&2; exit 2; }

source "$CLAUDE_PROJECT_DIR/.claude/hooks/_protected.sh"
input=$(cat)
tool=$(jq -r '.tool_name' <<<"$input")

deny() { echo "$1" >&2; exit 2; }

if [ "$tool" = "Bash" ]; then
  cmd=$(jq -r '.tool_input.command // ""' <<<"$input")
  # Writing data to a protected file
  if grep -Eq '(sed|perl) +-[a-z]*i|tee |mv |cp |rm ' <<<"$cmd" && grep -Eq "$PROTECTED_WORDS" <<<"$cmd"; then
    deny "Command rejected: It modifies a protected file. Explain why it's needed, and the user will make the change."
  fi

  if grep -Eq 'git( -C [^ ]+)* (reset|restore|clean)( |$)' <<<"$cmd"; then
    deny "Destructive git command: the user's uncommitted work lives in this tree."
  fi
  exit 0
fi

path=$(jq -r '.tool_input.file_path // ""' <<<"$input")
rel=${path#"$CLAUDE_PROJECT_DIR"/}
if grep -Eq "$PROTECTED" <<<"$rel"; then
  deny "$rel is read-only for the agent. Offer the diff to the user; they will apply it themselves."
fi
exit 0
