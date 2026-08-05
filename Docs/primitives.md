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
