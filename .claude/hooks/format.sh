#!/usr/bin/env bash
input=$(cat)
path=$(jq -r '.tool_input.file_path // ""' <<<"$input")
[[ "$path" == *.cs ]] || exit 0
rel=${path#"$CLAUDE_PROJECT_DIR"/}
# --folder mode, no MSBuild reload
dotnet format whitespace "$CLAUDE_PROJECT_DIR" --folder --include "$rel" >/dev/null 2>&1
exit 0
