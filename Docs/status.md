# Status — 2026-08-05

Snapshot of what exists, what is broken, and what to do next. Update this at the end of a work
session rather than mid-stream.

> **Everything below is UNVERIFIED.** All of it was written without ever compiling or running the
> project. Assume at least one thing does not compile on first import, and treat the checklist in
> "First thing to do" as the real gate.

---

## Where the project actually is

Phase 0. The **netcode spike — the hard go/no-go gate — has not started**: NGO, Unity Transport,
and Facepunch.Steamworks are all absent from the project. Everything built so far is the *"Dev B
builds now (safe)"* column of CLAUDE.md's Phase 0 section, plus the single grey-box test room that
section explicitly sanctions.

What that means in practice: the game is playable solo, single-player, and has never been near a
network.

---

## What is built

### The primitive engine (`Assets/Scripts/Core`, `Modifiers`, `Shared`)

The claim "a modifier is data, not code" is now true. `Shift.Core` is a standalone assembly holding
`PrimitiveState` (8 fields, all unmanaged so NGO can replicate it wholesale), `PrimitiveOverride`
(a bitmask plus values — one row of `Docs/primitives.md` with holes), `MassProfile`, and
`PrimitiveResolver`.

Resolution order is `baseline → intrinsic → fields → active modifier`. The player's equipped
modifier always wins; a field silently vetoing your choice would be an unguessable failure.

**Feel-neutrality is structural, not hoped-for.** `MassProfile.For(Normal)` is all 1.0 scales,
`Clamp01(0.6/0.6)` makes the movement lerp an exact overwrite, `1 + 0` leaves jump height alone —
so with no modifier equipped the motor reproduces its original arithmetic. A test asserts it.

### Four playable modifiers

| Key | Modifier | Primitives | What it does |
|---|---|---|---|
| 1 | Heavy | `MASS:heavy, FRICTION:0.9` | 140kg, jumps lower, carries 40kg, grips ice |
| 2 | Frog | `BOUNCE:0.9` | jump ×1.9, bounces on landing |
| 3 | Ghost | `PHASE:ghost` | passes through the Prop layer, still stands on floors |
| 4 | Feather | `MASS:tiny` | 8kg, faster, jumps higher, jetpack goes further |
| 0 | — | — | baseline, for A/B against unmodified feel |

Plus fields (LowGravity, HalfTime) and surfaces (Ice, Sticky, Trampoline) as `.asset` files.

### The player

Rigidbody capsule motor: walk, sprint, crouch, jump with coyote time and buffering, a jetpack
double-jump with a recharging gauge, velocity-driven object carrying with an outline highlight and
a contextual crosshair prompt.

### The run framework (`Assets/Scripts/Progression`, `Level`)

`Shift.Progression` is a second standalone assembly. The wall is the point: `RunDirector` **cannot**
reference the player, the primitive engine, or a collider — the compiler enforces it — so when it
becomes a `NetworkBehaviour` it drags nothing with it.

Start line, goal, checkpoints, kill volumes, respawn, prop reset, a run clock with splits, and
personal bests persisted as atomic-written JSON. `Split(index)` deliberately tolerates out-of-order
and skipped indices, and `ZoneGoal` checks no checkpoints — that is what "bless sequence breaks"
means in code, and it is very hard to retrofit.

### Zone 01 "Sluice" — the first real level

An Act 2 Combination level that **cannot be completed without swapping modifiers**, with every gate
enforced by code that already existed rather than by new tuning:

1. **Frog** — a 10m well whose only exit is 7.5m up. Jump plus a full jetpack burn falls ~3m short;
   Frog's landing bounce clears it. Gates on choice, not precision.
2. **Heavy** — a 30kg crate. `CarryMassLimit` is applied *before* targeting, so as anyone else the
   crosshair never even offers the prompt. The game says no without a tutorial.
3. **Ghost** — a Prop-layer grate. Heavy cannot pass it; Ghost could never have carried the crate.

Also a blessed **sequence break** (~25s faster, skips the whole puzzle via a Frog chain into the
low-gravity field that deliberately spills over the vault roof) and a **co-op route** that needs
zero extra code — a second player just stands on the plate.

### Tooling

`SHIFT/` menu: Build Movement Sandbox Scene, Build Primitive Sandbox Scene, Build Zone 01 (Sluice),
Repair Player In Open Scene, Validate Modifier Assets.

`Repair` is the important one — script edits reach existing components automatically, but newly
written components have to be *attached*, which is the usual reason a new feature looks broken.

---

## First thing to do — nothing here is verified

In order, stopping at the first failure:

1. **Let Unity compile.** Console clean, `SHIFT` menu present.
2. **Run EditMode tests.** Window → General → Test Runner. 5 suites, ~30 cases.
3. **Regression gate.** Open `MovementSandbox`, Play. The 1.5m step must still be jetpack-only and
   the crawl space must still need crouch. Any change is the manual-gravity substitution and must
   be chased down, not accepted — this scene is deliberately kept as the pristine feel baseline.
4. **`SHIFT/Build Zone 01 (Sluice)`** — the scene does not exist yet, only the builder that makes it.
5. **Play it.** Route above; `R` respawn, `Shift+R` restart, `Esc` pause.

---

## Known bugs and gaps

### Unimplemented by design (deferred, documented)

- **ADHESION and gravity *direction*** — Spider is designed but unbuilt. Both need re-orientation
  across the motor, ground cast and camera. CLAUDE.md warns adhesion modifiers may need rewriting
  once host-authoritative physics lands, so this waits for the spike.
- **CHARGE** resolves through the engine but nothing consumes it. No magnetism yet.
- **TIME_RATE** is applied to the player only. Field and prop time dilation are plumbed through the
  struct but not applied — per-entity time in one shared PhysX scene has no native support, and
  faking it would hide that. May eventually need a substepped physics scene.

### Real gaps that will bite

- **The player's `Rigidbody.mass` is a hardcoded 70 and does not track MASS.** A Heavy player shoves
  a crate exactly as hard as a Feather one. `MassPlate` works around it by reading `MassProfile`;
  anything else that needs a player's weight must do the same. Fixing it changes how carrying and
  shoving feel, so it needs its own tuning pass.
- **Zone 01's margins are calculated, not measured.** The well depth, the ledge height and the
  sequence-break chain are arithmetic against the motor's constants. Expect to nudge them.
- **The surface fix is a feel change.** Friction combine moved from `Maximum` to `Average` and the
  motor now samples the ground material. Baseline is preserved on unmarked geometry, but this is
  the one system that was previously verified feel-neutral and is no longer verified.
- **`UnityEngine.UI.Text` is legacy.** Fine for a debug HUD; the real loadout and menu UI should be
  TextMeshPro.
- **`Shader.Find("Universal Render Pipeline/Unlit")`** in `HighlightOutline` resolves in the editor
  but may not survive into a player build unless that shader is added to Always Included Shaders.
  The pickup outline would silently vanish in a build.

### Design decisions worth revisiting after playtesting

- **Pause costs you the run.** `Esc` releases the cursor and suppresses input but never touches
  `Time.timeScale`, and the clock keeps ticking. This kills pause-buffering exploits and avoids a
  solo-only code path that breaks the moment a host pauses a P2P session — but you cannot pause to
  answer the door. If it is hated, the escape hatch is one `bool` on `PauseMenuView`.
- **Friction combining by `Average` is asymmetric and permanent.** A surface can never fully
  cancel a player primitive. Intentional (player agency wins), but it removes a design lever.

### Process

- **`Scripts/Editor` has no owner** in CLAUDE.md's ownership table, and it now holds both asset
  authoring (Dev A's concern) and scene building (Dev B's). Assign it before it causes exactly the
  merge conflict that table exists to prevent.

---

## What to do next

**1. Verify what exists.** The checklist above. Nothing else is worth starting until the console is
clean and Zone 01 is playable.

**2. The netcode spike.** This is the actual gate and it has not begun. Install NGO + Facepunch
transport over Steam Datagram Relay, build the host/join test with ~20 rigidbodies, and run the
four pass criteria over real internet with 4 players. Everything downstream — real Zones, Spider,
anything expensive to rewrite — is held until this passes. The run framework touches no physics so
it survives whatever the spike decides; level *geometry* may not.

**3. Then, in rough value order:**
- **Spider / ADHESION** — the flagship missing modifier, but do it after the spike, not before.
- **CHARGE** — magnetism between props. A good clip-generator, no netcode decisions required.
- **Loadout screen** — currently number keys only. Needed before asymmetric locks or "co-op teeth"
  mean anything.
- **Player MASS affecting the world** — the `Rigidbody.mass` gap above.
- **Zones 02–08** — held until the spike passes. Phase 0 sanctions exactly one grey-box room and
  this is it.
