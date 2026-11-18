#!/usr/bin/env bash
input=$(cat)
cd "$CLAUDE_PROJECT_DIR" || exit 0
source .claude/hooks/_protected.sh

# Avoid an infinite loop of verification
code=2
[ "$(jq -r '.stop_hook_active // false' <<<"$input")" = "true" ] && code=1
fail() { echo "$1" >&2; exit $code; }

changed=$(git status --porcelain -uall | awk '{print $NF}')
[ -z "$changed" ] && exit 0

# 1.  Protected files that have been modified, regardless of the method
hit=$(grep -E "$PROTECTED" <<<"$changed")
[ -n "$hit" ] && fail "Protected files have been modified. Revert these changes and explain why they were necessary:
$hit"

# 2. Workarounds in Added Lines (Tracked Files + New Files)
pattern='#pragma warning disable|SuppressMessage|<NoWarn>|#nullable disable|Skip *=|null!|default!'
added=$(git diff HEAD -U0 -- '*.cs' '*.csproj' | grep -E '^\+[^+]')
for f in $(git ls-files -o --exclude-standard -- '*.cs' '*.csproj'); do added+=$'\n'$(cat "$f"); done
cheats=$(grep -E "$pattern" <<<"$added")
[ -n "$cheats" ] && fail "Workarounds are prohibited. Address the root cause instead of masking the symptom:
$cheats"

# 3.Removed tests
removed=$(git diff HEAD -U0 -- tests | grep -cE '^-\s*\[(Fact|Theory)')
new=$(git diff HEAD -U0 -- tests | grep -cE '^\+\s*\[(Fact|Theory)')
[ "$removed" -gt "$new" ] && fail "Existing tests have been removed ($removed removed, $new added). Restore them or ask the user for permission."

# 4. Strict build
out=$(dotnet build Interlinea.slnx -warnaserror --nologo -v q 2>&1) || fail "Build in failure :
$(tail -40 <<<"$out")"

# 5. unit tests + architecture
for p in tests/*.UnitTests; do
  out=$(dotnet test --project "$p" --no-build --no-progress 2>&1) || fail "Tests in failure in $p :
$(tail -60 <<<"$out")"
done
exit 0
