# SHIFT

Co-op (1–4 player) physics-puzzle game. Players equip a loadout of 4 "Reality
Modifiers" and swap between them to solve parkour/physics levels. Player-hosted
P2P, no dedicated servers. Target: Steam, $7.99.

Two-person team. Dev A = systems/netcode. Dev B = feel/content.

**Pitch:** *Portal's brain, Human Fall Flat's body, and a loadout screen.*

---

## Non-negotiable design rules

1. **Modifiers change PRIMITIVES, never stat numbers.** "+10% speed" is banned.
2. **A new modifier is DATA (a ScriptableObject), not new code.** If a modifier
   needs bespoke code, the primitive system is wrong — fix the system instead.
3. **Every modifier needs ≥3 documented interactions** with other modifiers, or
   it gets cut. A modifier that only works alone is dead weight.
4. **Interactions must be guessable from real-world intuition** (metal +
   electricity, ice + slippery). No tutorial should be needed for combos.
5. **Deterministic and local.** No LLM/AI calls at runtime, ever. No API costs.
6. **Physics is HOST-AUTHORITATIVE.** Clients interpolate. Never re-simulate.
7. **Solo must be viable**, but every Zone has a faster or hidden co-op route.
8. **~24 modifiers at launch.** Not 100. The rest are free post-launch updates.

---

## The 8 primitives (the entire game runs on these)

| Primitive | Range / values | Applies to |
|---|---|---|
| MASS | tiny / light / normal / heavy / giant | player, prop |
| FRICTION | 0.0 (ice) → 1.0 (sticky) | player, prop, surface |
| BOUNCE | 0.0 (dead) → 0.95 (super-ball) | player, prop, surface |
| GRAVITY | Vector3 direction + strength multiplier | player, zone, prop |
| ADHESION | on / off (sticks to any surface) | player |
| PHASE | solid / ghost (no collision with props) | player, prop |
| CHARGE | none / magnetic+ / magnetic− / electric | player, prop, surface |
| TIME_RATE | 0.0 (frozen) → 1.0 (normal) → 2.0 | player, zone, prop |

**Modifiers are just bundles of these:**
- Spider = `ADHESION:on, MASS:light`
- Frog = `BOUNCE:0.9, jump impulse ×2`
- Heavy = `MASS:heavy, FRICTION:0.9`

If you can't write a modifier as a row of values, redesign it.

---

## Tech stack

- **Unity 6** — exact version pinned in `ProjectSettings/ProjectVersion.txt`.
  Both devs MUST match, down to the last digit.
- **Netcode for GameObjects (NGO)** — free, Unity-maintained. Same stack as
  Lethal Company.
- **Facepunch.Steamworks** transport over **Steam Datagram Relay** — free relay,
  hides player IPs, NAT punch-through with automatic relay fallback. Chosen over
  Photon specifically to avoid metered bandwidth costs.
- **Steam App ID 480** during development.
- Alternative if NGO physics disappoints: **FishNet** (free, MIT, stronger
  built-in physics prediction).

---

## Ownership — avoid merge conflicts

**Dev A owns:**
`Scripts/Core`, `Scripts/Net`, `Scripts/Modifiers`, `Scripts/Progression`,
`Scripts/Leaderboard`, `Scenes/Sandbox`

**Dev B owns:**
`Scripts/Player`, `Scripts/Level`, `Scripts/UI`, `Scripts/Audio`,
`Scenes/Zones`, `Scenes/Menus`, `Prefabs/`, `Art/`, `Data/Modifiers` (tuning)

**Both:** `Scripts/Shared`, `Docs/`

**HARD RULE:** never edit the same `.unity` scene or `.prefab` as the other dev.
Unity YAML does not merge. Coordinate in chat before touching the other person's
folders.

The primitive engine is the clean interface between A and B — B authors content
as data without touching A's netcode.

---

## Current phase: PHASE 0 — Netcode Spike

**Goal:** prove player-hosted physics sync works before building anything real.

**Build:** one test scene — floor, ~20 physics cubes, basic capsules that walk,
jump, and shove. NGO + Facepunch transport, host/join via App ID 480.

**Test:** over real internet, NOT the same wifi.

**Pass criteria:**
- [ ] 4 players connect over real internet
- [ ] ~20 rigidbodies synced, no visible jitter on clients
- [ ] A player can stand on a box another player is pushing
- [ ] No desync after 10 minutes

**Pass → start the real game. Fail → fix the netcode approach now, while
switching is cheap.**

Budget: ~3–5 days of work, 2–4 weeks calendar (buffer for real-world lag bugs).

### Parallel work during Phase 0

**Dev B builds now (safe):** character controller, camera, grab/throw, jump feel,
the 8 primitives applied locally single-player, modular physics prop kit, one
grey-box test room, loadout UI mockup.

Progress on that list (detail + known bugs in `Docs/status.md`):
- [x] Character controller, camera, grab/carry, jump feel
- [x] Primitive engine — 6 of 8 primitives live (ADHESION and CHARGE are not)
- [x] One grey-box test room — Zone 01 "Sluice"
- [x] Run framework: timer, splits, checkpoints, respawn, personal bests
- [ ] Loadout UI — number keys only so far
- [ ] Netcode spike — **not started.** NGO is not installed.

**Held until the spike passes:** real levels, modifiers that spawn/destroy
objects mid-run, anything expensive to rewrite.

*Why:* if netcode forces host-authoritative rigidbodies, every adhesion-based
modifier (Spider, Magnet, Ghost) may need rewriting. Don't author 20 Zones on
physics that might change.

---

## Roadmap

| Phase | When | What |
|---|---|---|
| 0 | Weeks 1–6 | Netcode spike. **Hard go/no-go gate.** |
| 1 | Months 2–4 | MVP: 8 modifiers, 6–8 Zones, 4-player P2P, loadout + swap |
| 2 | Month 5 | Steam page live (wishlists start accruing), demo build |
| 3 | Months 6–9 | Scale to ~24 modifiers, 20–30 Zones, skill trees, leaderboards |
| 4 | Month 10 | Steam Next Fest + closed beta |
| 5 | Months 11–12 | Launch, $7.99 with launch discount |
| 6 | Ongoing | Free modifier "seasons", Weekly Challenge, then level editor |

---

## Design reference

**Level progression** — difficulty rises via cognitive/coordination load, never
harder jumps:
- Act 1 Literacy: one modifier in isolation
- Act 2 Combination: swap between two of your own
- Act 3 Coordination: your power sets up your teammate's move
- Act 4 Systems: multi-stage, timed, communicative

**Co-op teeth** (prevents "everyone brings the same build"):
asymmetric locks, 4-slot loadout scarcity, assist-only verbs (Web-Tether,
Balloon-lift, Coil-launch, Swap-Places), physical trust moments, comms puzzles.

**Clip moments are a feature, not a side effect.** Engineer at least three
guaranteed clip-generators. The genre sells on friends failing together.

**Speedrunning:** built-in timer/splits/leaderboards, multiple routes, bless
sequence breaks rather than patching them.

---

## Setup notes (already decided)

- Unity project created first, then `git init` inside it, then pushed to the
  existing GitHub repo.
- Git LFS for all binaries (see `.gitattributes`).
- Unity → Edit → Project Settings → Editor →
  Version Control: **Visible Meta Files**, Asset Serialization: **Force Text**.
- Dev A uses JetBrains Rider + Claude Code plugin.
- Dev B uses VS Code + Claude Code extension.

---

## Conventions

- **Zone** = a level. **Field** = a trigger volume that imposes GRAVITY or TIME_RATE on whatever
  is inside it. These were the same word once and it collided badly — `Assets/Data/Zones` held
  physics volumes while `Assets/Scenes/Zones` held levels. Keep them distinct.
- C#. PascalCase public, `_camelCase` private fields.
- One MonoBehaviour per file, filename == class name.
- Modifier definitions live in `Assets/Data/Modifiers/<Category>/` as
  ScriptableObjects.
- Prefer composition over inheritance for modifier effects.
- Full design doc and modifier table: `Docs/`
