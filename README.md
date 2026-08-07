# SHIFT

Co-op physics-puzzle game. See CLAUDE.md for design rules, Docs/ for details.

**Current state: `Docs/status.md`** — what's built, what's broken, what's next.

## Setup
1. Install the EXACT Unity version in `ProjectSettings/ProjectVersion.txt`
2. `git lfs install` (once per machine)
3. Clone, then Unity Hub -> Add project from disk -> select this folder
4. First open takes several minutes while Unity rebuilds Library/

## Play it
- **SHIFT -> Build Zone 01 (Sluice)** then press Play. That's the first real level.
- **SHIFT -> Build Movement Sandbox Scene** is the feel baseline — keep it pristine.
- **SHIFT -> Build Primitive Sandbox Scene** is where each primitive can be falsified on its own.
- **SHIFT -> Repair Player In Open Scene** attaches anything a scene is missing. Run this first
  whenever a feature looks broken after pulling — a script edit updates existing components, but a
  *new* component still has to be attached to something.

Controls: WASD, shift sprint, ctrl crouch, space jump (again midair for the jetpack), left click
carry, `1`-`4` swap modifier, `0` baseline, `R` respawn, `Shift+R` restart run, `Esc` pause.

## Vocabulary
- **Zone** = a level. **Field** = a trigger volume imposing GRAVITY or TIME_RATE on what's inside.
- **Modifier** = a bundle of primitive values, authored as a ScriptableObject. Never code.

## Don't
- Don't commit Library/ (gitignored)
- Don't edit the other dev's scenes/prefabs — they don't merge
- Don't add a modifier as bespoke code — it's a ScriptableObject

the greatest modifier physics co-op friendslop game OAT. credit to claude for the coding.
