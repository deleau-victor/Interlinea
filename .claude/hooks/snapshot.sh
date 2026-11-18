#!/usr/bin/env bash
input=$(cat)
cd "$CLAUDE_PROJECT_DIR" || exit 0
source .claude/hooks/_snapshot.sh

dir=$(snapshot_dir "$(jq -r '.session_id' <<<"$input")")
mkdir -p "$dir"
worktree_tree "$dir/index.before" > "$dir/before"
exit 0
