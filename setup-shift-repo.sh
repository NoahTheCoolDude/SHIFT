#!/usr/bin/env bash
# SHIFT — repo scaffolding
# Run from the ROOT of your git repo, AFTER Unity Hub has created the project there.
#   chmod +x setup-shift-repo.sh && ./setup-shift-repo.sh
# Safe to re-run; it never overwrites existing files.

set -euo pipefail

if [ ! -d "Assets" ]; then
  echo "!! No Assets/ folder here. Create the Unity project in this directory first."
  exit 1
fi

echo "==> Creating folder structure"

# --- Dev A territory: systems, netcode ---
mkdir -p Assets/Scripts/Core          # primitive engine (mass, friction, bounce...)
mkdir -p Assets/Scripts/Net           # netcode, lobby, host authority
mkdir -p Assets/Scripts/Modifiers     # modifier system + runtime application
mkdir -p Assets/Scripts/Progression   # unlocks, loadouts, save data
mkdir -p Assets/Scripts/Leaderboard   # timer, splits, Steam leaderboards

# --- Dev B territory: feel, content, presentation ---
mkdir -p Assets/Scripts/Player        # controller, camera, grab/throw
mkdir -p Assets/Scripts/Level         # zone logic, checkpoints, hazards
mkdir -p Assets/Scripts/UI            # loadout screen, HUD, menus
mkdir -p Assets/Scripts/Audio

mkdir -p Assets/Scripts/Shared        # both — interfaces, enums, constants
mkdir -p Assets/Scripts/Editor        # editor tooling
mkdir -p Assets/Tests/PlayMode
mkdir -p Assets/Tests/EditMode

# --- Data: modifiers as ScriptableObjects, NOT code ---
mkdir -p Assets/Data/Modifiers/Player
mkdir -p Assets/Data/Modifiers/Physics
mkdir -p Assets/Data/Modifiers/Environment
mkdir -p Assets/Data/Modifiers/WorldRule
mkdir -p Assets/Data/Zones

# --- Scenes: keep these separated to avoid merge hell ---
mkdir -p Assets/Scenes/Sandbox        # Dev A: netcode spike, physics tests
mkdir -p Assets/Scenes/Zones          # Dev B: actual levels
mkdir -p Assets/Scenes/Menus

# --- Prefabs ---
mkdir -p Assets/Prefabs/Player
mkdir -p Assets/Prefabs/Props         # the modular physics prop kit
mkdir -p Assets/Prefabs/Modifiers
mkdir -p Assets/Prefabs/UI
mkdir -p Assets/Prefabs/Zones         # reusable room chunks

# --- Art / Audio ---
mkdir -p Assets/Art/Materials
mkdir -p Assets/Art/Models
mkdir -p Assets/Art/Textures
mkdir -p Assets/Art/VFX
mkdir -p Assets/Art/Shaders
mkdir -p Assets/Audio/SFX
mkdir -p Assets/Audio/Music

mkdir -p Assets/Settings                # render pipeline, input actions
mkdir -p Assets/Plugins                 # Facepunch.Steamworks etc.
mkdir -p Assets/StreamingAssets

# --- Docs (outside Assets so Unity ignores them) ---
mkdir -p Docs
mkdir -p Docs/playtests

# Keep empty dirs in git
find Assets Docs -type d -empty -exec touch {}/.gitkeep \;

echo "==> Writing .gitignore"
if [ ! -f .gitignore ]; then
cat > .gitignore << 'EOF'
# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/

# Never commit these
/[Aa]ssets/AssetStoreTools*
/[Aa]ssets/Plugins/Editor/JetBrains*
.vs/
.vscode/
.idea/
*.csproj
*.unityproj
*.sln
*.suo
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# OS
.DS_Store
Thumbs.db
desktop.ini

# Crash reports
sysinfo.txt
crashlytics-build.properties

# Builds / packages
*.apk
*.aab
*.unitypackage
*.app
ExportedObj/

# Claude local scratch (keep CLAUDE.md, ignore local settings)
.claude/settings.local.json
EOF
else
  echo "    .gitignore exists, skipped"
fi

echo "==> Writing .gitattributes (Git LFS for binaries)"
if [ ! -f .gitattributes ]; then
cat > .gitattributes << 'EOF'
* text=auto

# Unity text assets — force diffable
*.cs      text diff=csharp
*.shader  text
*.meta    text merge=unityyamlmerge eol=lf
*.unity   text merge=unityyamlmerge eol=lf
*.asset   text merge=unityyamlmerge eol=lf
*.prefab  text merge=unityyamlmerge eol=lf
*.mat     text merge=unityyamlmerge eol=lf
*.anim    text merge=unityyamlmerge eol=lf
*.controller text merge=unityyamlmerge eol=lf

# Binaries via LFS
*.psd   filter=lfs diff=lfs merge=lfs -text
*.png   filter=lfs diff=lfs merge=lfs -text
*.jpg   filter=lfs diff=lfs merge=lfs -text
*.tga   filter=lfs diff=lfs merge=lfs -text
*.exr   filter=lfs diff=lfs merge=lfs -text
*.fbx   filter=lfs diff=lfs merge=lfs -text
*.blend filter=lfs diff=lfs merge=lfs -text
*.obj   filter=lfs diff=lfs merge=lfs -text
*.wav   filter=lfs diff=lfs merge=lfs -text
*.mp3   filter=lfs diff=lfs merge=lfs -text
*.ogg   filter=lfs diff=lfs merge=lfs -text
*.dll   filter=lfs diff=lfs merge=lfs -text
EOF
else
  echo "    .gitattributes exists, skipped"
fi

echo "==> Writing CLAUDE.md"
if [ ! -f CLAUDE.md ]; then
cat > CLAUDE.md << 'EOF'
# SHIFT

Co-op (1-4p) physics-puzzle game. Players equip a loadout of 4 "Reality Modifiers"
and swap between them to solve parkour/physics levels. Player-hosted P2P, no
dedicated servers. Target: Steam, $7.99.

## Non-negotiable design rules
1. Modifiers change PRIMITIVES, never stat numbers. "+10% speed" is banned.
2. A new modifier is DATA (a ScriptableObject), not new code. If a modifier
   needs bespoke code, the primitive system is wrong — fix the system instead.
3. Every modifier must have >=3 documented interactions with other modifiers,
   or it gets cut.
4. Interactions should be guessable from real-world intuition (metal+electricity,
   ice+slippery). No tutorial needed for combos.
5. Deterministic and local. No LLM/AI calls at runtime, ever.
6. Physics is HOST-AUTHORITATIVE. Clients interpolate. Never re-simulate.

## The 8 primitives (the whole game)
MASS, FRICTION, BOUNCE, GRAVITY (vector+strength), ADHESION, PHASE, CHARGE, TIME_RATE

Examples:
- Spider = ADHESION:on, MASS:light
- Frog   = BOUNCE:0.9, jump impulse x2
- Heavy  = MASS:heavy, FRICTION:0.9

## Tech stack
- Unity 6 (exact version pinned in ProjectSettings/ProjectVersion.txt — both devs MUST match)
- Netcode for GameObjects (NGO)
- Facepunch.Steamworks transport, Steam Datagram Relay (free, hides IPs)
- Steam App ID 480 during development

## Ownership (avoid merge conflicts)
Dev A: Scripts/Core, Scripts/Net, Scripts/Modifiers, Scripts/Progression,
       Scripts/Leaderboard, Scenes/Sandbox
Dev B: Scripts/Player, Scripts/Level, Scripts/UI, Scripts/Audio,
       Scenes/Zones, Scenes/Menus, Prefabs/, Art/, Data/Modifiers (tuning)
Both:  Scripts/Shared, Docs/

RULE: never edit the same .unity scene or .prefab as the other dev. They do not
merge. Coordinate in chat before touching the other person's folders.

## Current phase
PHASE 0 — netcode spike. Goal: 4 players + ~20 synced rigidbodies over real
internet with no jitter, no desync after 10 min. Do NOT author real levels until
this passes.

## Conventions
- C#, PascalCase for public, _camelCase for private fields.
- One MonoBehaviour per file, filename == class name.
- Modifier definitions go in Assets/Data/Modifiers/<Category>/ as ScriptableObjects.
- Prefer composition over inheritance for modifier effects.
EOF
else
  echo "    CLAUDE.md exists, skipped"
fi

echo "==> Writing Docs stubs"
[ -f Docs/primitives.md ] || cat > Docs/primitives.md << 'EOF'
# Primitives — the source of truth

| Primitive | Range / values | Applies to | Notes |
|---|---|---|---|
| MASS      | tiny / light / normal / heavy / giant | player, prop | |
| FRICTION  | 0.0 (ice) .. 1.0 (sticky) | player, prop, surface | |
| BOUNCE    | 0.0 (dead) .. 0.95 (super-ball) | player, prop, surface | |
| GRAVITY   | Vector3 dir + float strength | player, zone, prop | |
| ADHESION  | on / off | player | sticks to any surface |
| PHASE     | solid / ghost | player, prop | ghost = no collision w/ props |
| CHARGE    | none / mag+ / mag- / electric | player, prop, surface | |
| TIME_RATE | 0.0 (frozen) .. 2.0 | player, zone, prop | |

## Launch modifiers (target: ~24)
Fill in as they're designed. Each row = a ScriptableObject.

| Name | Category | Primitive bundle | 3 interactions | Status |
|---|---|---|---|---|
| Spider | Player | ADHESION:on, MASS:light | +SidewaysG, +Wind, +Ice | design |
| Frog   | Player | BOUNCE:0.9, jumpx2 | +BounceWorld, +LowG, +Sticky | design |
| Heavy  | Player | MASS:heavy, FRICTION:0.9 | +Seesaw, +Ice, +Magnet | design |
EOF

[ -f Docs/netcode-notes.md ] || cat > Docs/netcode-notes.md << 'EOF'
# Phase 0 — Netcode Spike Log

## Pass criteria
- [ ] 4 players connect over real internet (NOT same wifi)
- [ ] ~20 rigidbodies synced, no visible jitter on clients
- [ ] Player can stand on a box another player is pushing
- [ ] No desync after 10 minutes
- [ ] Host migration behaviour understood (even if unhandled)

## Test log
| Date | Build | Players | Result | Notes |
|---|---|---|---|---|
EOF

[ -f Docs/playtests/TEMPLATE.md ] || cat > Docs/playtests/TEMPLATE.md << 'EOF'
# Playtest — YYYY-MM-DD
Players: 
Build: 
Zone(s): 

## What worked
## What confused people
## Where they laughed  <-- this is the clip moment, protect it
## Bugs
## Actions
EOF

echo "==> Writing README.md"
[ -f README.md ] || cat > README.md << 'EOF'
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
EOF

echo ""
echo "==> Done. Next:"
echo "   git lfs install"
echo "   git add -A && git commit -m 'Scaffold project structure' && git push"
echo ""
echo "   Then in Unity: Edit > Project Settings > Editor >"
echo "     Version Control: Visible Meta Files"
echo "     Asset Serialization: Force Text"
echo "   (both required for git to work sanely — commit after changing)"
