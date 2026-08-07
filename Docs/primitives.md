# Primitives — the source of truth

> **Vocabulary.** A **Zone** is a level (CLAUDE.md: "6–8 Zones"). A **Field** is a trigger volume
> that imposes GRAVITY or TIME_RATE on whatever is inside it. The code used to call fields "zones"
> and the two meanings collided in `Assets/Data/Zones`; fields now live in `Assets/Data/Fields`
> and `Assets/Data/Zones` holds level manifests.
> *Suggested for CLAUDE.md's Conventions section — not added unilaterally, since that file is the
> binding contract.*

| Primitive | Range / values | Applies to | Notes |
|---|---|---|---|
| MASS      | tiny / light / normal / heavy / giant | player, prop | |
| FRICTION  | 0.0 (ice) .. 1.0 (sticky) | player, prop, surface | |
| BOUNCE    | 0.0 (dead) .. 0.95 (super-ball) | player, prop, surface | |
| GRAVITY   | Vector3 dir + float strength | player, field, prop | |
| ADHESION  | on / off | player | sticks to any surface |
| PHASE     | solid / ghost | player, prop | ghost = no collision w/ props |
| CHARGE    | none / mag+ / mag- / electric | player, prop, surface | |
| TIME_RATE | 0.0 (frozen) .. 2.0 | player, field, prop | |

## Launch modifiers (target: ~24)
Fill in as they're designed. Each row = a ScriptableObject.

| Name | Category | Primitive bundle | 3 interactions | Status |
|---|---|---|---|---|
| Spider  | Player | ADHESION:on, MASS:light | +SidewaysG, +Wind, +Ice | deferred |
| Frog    | Player | BOUNCE:0.9 | +Trampoline, +LowG, +Sticky | built |
| Heavy   | Player | MASS:heavy, FRICTION:0.9 | +Seesaw, +Ice, +Magnet | built |
| Ghost   | Player | PHASE:ghost | +Grates, +Heavy, +Crusher | built |
| Feather | Player | MASS:tiny | +LowG, +Seesaw, +Frog | built |

**Frog no longer carries a "jump ×2".** That was a raw stat multiplier with no primitive behind
it, which rule 1 bans. BOUNCE now drives jump height in the engine — `jumpHeight × (1 + BOUNCE)`,
so Frog gets 1.9× for free and a bouncy thing launching harder stays intuitive.

**Spider is deferred, not cut.** It needs ADHESION, which requires gravity-direction re-orientation
across the motor, ground cast and camera. CLAUDE.md warns adhesion modifiers may need rewriting
once host-authoritative physics lands, so it waits for the netcode spike.

## Engine notes

- `MASS` becomes consequences via `MassProfile` (`Assets/Scripts/Core/MassProfile.cs`): kilograms
  plus speed, jump, thrust and carry scales. That table is code, never per-modifier data —
  the moment it is authorable, every modifier can smuggle in "+10% speed".
- Surfaces do **not** enter the resolution stack. Surface FRICTION/BOUNCE become a PhysicsMaterial;
  **friction combines by `Average`, bounce by `Maximum`.** Average because `Maximum` made ice inert
  — `max(0.6, 0.0)` is just normal friction, so only an already-slippery entity would ever notice.
  Averaging gives ice+normal 0.30 and ice+Heavy 0.45, so a modifier can meaningfully answer a
  surface without cancelling it. Bounce stays `Maximum` because one bouncy party is enough.
- The motor samples the ground's PhysicsMaterial in `CheckGrounded` and combines it into an
  *effective* friction/bounce. Without that, surfaces would not affect the player at all: the motor
  overwrites velocity every step, so PhysX friction on the capsule never gets a say. Unmarked
  geometry has no material and falls through to the raw primitive.
- Resolution order is `baseline → intrinsic → fields → active modifier`. The player's equipped
  modifier always wins.
- `TIME_RATE` is applied to the player only. Field and prop time dilation are plumbed through the
  struct but not applied — per-entity time in one shared PhysX scene has no native support.
- `CHARGE` is defined and resolved but nothing consumes it yet.

## Known gaps

- **The player's `Rigidbody.mass` is a hardcoded 70 and does not track MASS.**
  `RigidbodyPrimitiveApplier` is prop-only, so a Heavy player shoves a crate exactly as hard as a
  Feather one. Fixing it changes how carrying and shoving feel, so it needs its own tuning pass.
  Anything that needs a player's weight must read `MassProfile`, not `Rigidbody.mass` — see
  `MassPlate`.
- **ADHESION and gravity *direction* are unimplemented.** Deferred with Spider until after the
  netcode spike.
