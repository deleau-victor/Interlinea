# Tree of the working tree (tracked + untracked, not including excluded files by .gitignore), without touching the real index, HEAD or any file.
worktree_tree() {
  local index="$1"
  cp "$(git rev-parse --git-path index)" "$index" 2>/dev/null || rm -f "$index"
  GIT_INDEX_FILE="$index" git add -A >/dev/null 2>&1
  GIT_INDEX_FILE="$index" git write-tree
}

snapshot_dir() { echo "${TMPDIR:-/tmp}/interlinea-claude/$1"; }
