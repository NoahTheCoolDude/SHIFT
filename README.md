# SHIFT

Co-op physics-puzzle game. See CLAUDE.md for design rules, Docs/ for details.

## Setup
1. Install the EXACT Unity version in `ProjectSettings/ProjectVersion.txt`
2. `git lfs install` (once per machine)
3. Clone, then Unity Hub -> Add project from disk -> select this folder
4. First open takes several minutes while Unity rebuilds Library/

## Don't
- Don't commit Library/ (gitignored)
- Don't edit the other dev's scenes/prefabs — they don't merge
- Don't add a modifier as bespoke code — it's a ScriptableObject
