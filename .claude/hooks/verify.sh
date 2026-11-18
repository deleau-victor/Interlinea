#!/usr/bin/env bash
input=$(cat)
cd "$CLAUDE_PROJECT_DIR" || exit 0
source .claude/hooks/_protected.sh
source .claude/hooks/_snapshot.sh

# Avoid an infinite loop of verification
code=2
[ "$(jq -r '.stop_hook_active // false' <<<"$input")" = "true" ] && code=1
fail() { echo "$1" >&2; exit $code; }

dir=$(snapshot_dir "$(jq -r '.session_id' <<<"$input")")
mkdir -p "$dir"
# No snapshot (hook missing, odd session): fall back to the old behaviour.
before=$(cat "$dir/before" 2>/dev/null || git rev-parse 'HEAD^{tree}')
after=$(worktree_tree "$dir/index.after")

# What the agent changed during THIS turn, untracked files included.
changed=$(git diff --name-only "$before" "$after")
[ -z "$changed" ] && exit 0

# 1. Protected files touched by the agent
hit=$(grep -E "$PROTECTED" <<<"$changed")
[ -n "$hit" ] && fail "You modified protected files during this turn:
$hit
Do NOT restore them yourself: a checkout would also erase the user's uncommitted work.
List them, explain why, and stop. The user will restore them."

# 2. Workarounds: against HEAD on purpose, a workaround is one whoever wrote it
pattern='#pragma warning disable|SuppressMessage|<NoWarn>|#nullable disable|Skip *=|null!|default!'
added=$(git diff HEAD -U0 -- '*.cs' '*.csproj' | grep -E '^\+[^+]')
for f in $(git ls-files -o --exclude-standard -- '*.cs' '*.csproj'); do added+=$'\n'$(cat "$f"); done
cheats=$(grep -E "$pattern" <<<"$added")
[ -n "$cheats" ] && fail "Workarounds are prohibited. Address the root cause instead of masking the symptom:
$cheats"

# 3. Tests removed by the agent during this turn
removed=$(git diff -U0 "$before" "$after" -- tests | grep -cE '^-\s*\[(Fact|Theory)')
new=$(git diff -U0 "$before" "$after" -- tests | grep -cE '^\+\s*\[(Fact|Theory)')
[ "$removed" -gt "$new" ] && fail "Tests removed during this turn ($removed removed, $new added). Restore them or ask the user for permission."

# 4. Strict build
out=$(dotnet build Interlinea.slnx -warnaserror --nologo -v q 2>&1) || fail "Build in failure :
$(tail -40 <<<"$out")"

# 5. unit tests + architecture
for p in tests/*.UnitTests; do
  out=$(dotnet test --project "$p" --no-build --no-progress 2>&1) || fail "Tests in failure in $p :
$(tail -60 <<<"$out")"
done
exit 0
