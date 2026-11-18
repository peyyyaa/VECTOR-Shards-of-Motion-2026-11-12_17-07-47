# Shards of Motion — Prototype Status Report

**Date:** 2026-11-17
**Build:** `Prototype_Room01` (graybox), Unity 6000.3.22f1, URP 2D Renderer
**Purpose:** describe how every system currently works, with the physics behind it, so the team and the physics reviewer can validate it.

> **Note:** Settings marked "verified" were checked in the saved project files, not in play. The push-and-pull redesign (Wind Tether, Burst no longer launching players, plate resting check) has **not been play-tested yet**. Use the table in Section 7 to record what you observe.

---

## 1. Global physics setup

| Setting | Value | Notes |
|---|---|---|
| Units | 1 Unity unit = 1 m | One graybox tile = 1 × 1 m |
| World gravity *g* | **29.43 m/s²** (3 × Earth's 9.81) | Project Settings → Physics 2D. Chosen so jumps feel snappy. |
| Gravity Scale | **1 on every body** | Everything falls at the same rate, as in reality. Abilities change it only briefly (float, dash). |
| Physics step | 0.02 s (50 Hz) | Unity simulates in fixed steps, so results are close to ideal but not exact. |
| Air resistance | None (Linear Damping 0) | Except the float's fall-speed cap (see 2.3). |
| Friction | Unity 2D uses **one** coefficient for static and kinetic friction | Real surfaces usually need more force to *start* sliding. |

**Free-fall check (Galileo):** a player and a block dropped from the same height land together. From 4.5 m: t = √(2h/g) = √(9 / 29.43) ≈ **0.55 s**.

---

## 2. Player mechanics (both players)

### 2.1 Walking
- Speed **6 m/s**. The script *sets* horizontal velocity directly instead of applying a force, for tight control.
- **Riding:** when standing on a moving block, its horizontal speed is added to yours. (Real explanation: friction under your feet would drag you along. Simplification: the player's collider is frictionless, so the speed is added directly.)

### 2.2 Jump
- Height **h = 2.5 m**. Launch speed v = √(2gh) = √(2 × 29.43 × 2.5) ≈ **12.1 m/s**. Time to peak v/g ≈ **0.41 s**.
- Applied as an impulse J = m·v (ForceMode2D.Impulse). Vertical velocity is zeroed first, so every jump reaches the same height.
- Only when grounded: a thin box under the feet touching Ground or PhysicsObject layers.

### 2.3 Float (hold Jump while falling)
- Gravity drops to **1/6** while held: 29.43 / 6 ≈ **4.9 m/s²**. Newton's 2nd law: a = F/m = mg/m = g, so lower g means slower acceleration. (Fun fact: 1/6 is roughly Moon vs. Earth.)
- Fall speed is capped at **2 m/s**. This stands in for terminal velocity. **Simplification:** real drag grows gradually with speed; here the speed is simply clamped.
- Lasts up to **1 s** per airtime, refilled on landing. Falls about 1.6 m in one second of floating.

### 2.4 Dash
- **3 m in about 0.16 s**, horizontal, gravity off. Constant velocity: v = d/t = 3 / 0.16 = **18.75 m/s**.
- 0.15 s is rounded to whole physics steps (8 × 0.02 s), and the speed is calculated from the rounded time, so the distance stays exactly 3 m.
- Sets velocity directly (independent of mass). Cooldown 0.5 s. One dash per airtime.
- Stops flush at blocks instead of ramming them.

### 2.5 Pushing blocks (walking into them)
- Each player pushes with a continuous **12 N** force (ForceMode2D.Force) while grounded and touching the block.
- The block only moves if total push > friction: **F_net = ΣF_push − μmg**, then a = F_net / m.
- 2 kg block, μ = 1/3: friction = (1/3)(2)(29.43) ≈ **19.6 N**.
  - One player: 12 N < 19.6 N → **doesn't move**.
  - Both players: 24 N > 19.6 N → **moves**, a = (24 − 19.6) / 2 ≈ **2.2 m/s²**.
- **Simplification:** a real person can only push because their feet grip the floor. The player's collider is frictionless, so the grip is assumed.

---

## 3. Kinetic Burst (Player 1 only, key F / gamepad West)

### 3.1 On objects
- Every dynamic object within **2.5 m** in a **60° cone** in front of P1 receives impulse **J = 15 N·s**, aimed straight away from P1.
- **Δv = J / m:** 2 kg block → **7.5 m/s**. A 1 kg object would get 15 m/s, and a 3 kg object 5 m/s.
- Then friction stops it: stopping distance d = v² / (2μg) = 7.5² / (2 × ⅓ × 29.43) ≈ **2.9 m**.
- Cooldown **0.5 s**.

### 3.2 Recoil (Newton's 3rd law)
- Each object hit pushes back on P1 with −J. **Simplification:** P1 receives only **15%** of it (2.25 m/s per target), a deliberate choice to keep control comfortable. 100% would be physically exact.
- If nothing is hit, there's no recoil (nothing to push back).
- P1's steering pauses for 0.15 s so the recoil is visible.

### 3.3 Players are never affected
- Kinetic Burst ignores both players, even if the Player layer is ticked by mistake.
- Launching players is no longer an ability. Future spring pads and kinetic cannons will use the existing `PlayerMovement.TryLaunch()` (projectile flight, no control until landing), which is kept in the code but currently unused.

---

## 3b. Wind Tether (Player 2 only, hold the Ability button)

- **Targeting (on press):** the nearest Dynamic object on PhysicsObject within **4 m**, inside a **120° cone** in front of P2, with clear line of sight. Players, static and kinematic bodies are never targeted.
- **While held:** a continuous **24 N** pull toward P2 (ForceMode2D.Force, at the object's center of mass). Objects are never teleported or positioned directly.
- **Stops when:** the button is released, the target goes beyond 4 m, or line of sight is blocked (Ground, PhysicsObject, Interactable solid colliders). Press again to re-target.
- **Hold Distance 1 m:** the pull pauses (tether stays attached) when the object is within 1 m, so it isn't dragged into P2.
- **Physics (Newton's 2nd law with friction):** 2 kg block: friction μmg = (1/3)(2)(29.43) ≈ **19.6 N**. Net force 24 − 19.6 ≈ **4.4 N**, so a = 4.4 / 2 ≈ **2.2 m/s²**. Pulled from rest over 1.6 m, it reaches v = √(2 × 2.2 × 1.6) ≈ 2.65 m/s, then slides v² / 2μg ≈ 0.36 m after release.
- **Rope direction:** the force acts along the line from the object to P2. If P2 is higher, part of the pull is upward, which lowers the normal force and so the friction (like pulling a sled with an angled rope).
- **Simplification:** a real rope pulls the player toward the object just as hard (Newton's 3rd law). P2 is assumed braced, so no reaction is applied to P2.
- **Visual:** a LineRenderer from P2 to the target while tethered (Scene-view gizmos show range and cone).

---

## 4. Puzzle objects

### 4.1 Pressure plate
- Thin trigger zone on the floor. Adds up weight **W = Σmg** (world g, ignoring temporary Gravity Scale changes).
- **Counts stacks:** bodies in the zone, plus anything resting on them, recursively. Normal force passes down through the stack.
- Threshold **45 N**:

  | On the plate | Weight | Pressed? |
  |---|---|---|
  | One player | 29.4 N | No |
  | Both players | 58.9 N | Yes |
  | 2 kg block | 58.9 N | Yes |
  | Block + player on top of it | 88.3 N | Yes |

- **Resting check:** a body in the plate's zone only counts when moving slower than **0.5 m/s**, so a block sliding across, or a player walking over, doesn't press it. Bodies stacked on a resting body count as before.
- **Simplifications:** a real scale measures normal force, which equals mg only at rest. A body bouncing on top of a resting stack still counts. A body partly resting elsewhere counts with full weight.

### 4.2 Exit door
- Solid while the plate is released, open (faded, passable) while the plate is pressed.
- The room completes when **both players** are in the open doorway. The Console logs "Room complete!".

### 4.3 The current room's intended solution (push and pull)
Both players or the block can open the door, but both players must leave through it, so the block must hold the plate.
1. P1 (left side) uses Kinetic Burst: the 2 kg block slides ~2.9 m toward the plate but stops short.
2. P2 (standing beyond the plate, near the door) uses Wind Tether to pull the block the rest of the way.
3. P2 releases; the block slides a little and settles on the plate (< 0.5 m/s).
4. The door opens; both players walk into the doorway.

Intended layout (block start x = −2, plate x = 2.5, P2 start x = 4.6): after one burst the block's center is at ~0.9 m, 3.7 m from P2, which is inside tether range. **Not enforced:** two bursts, or both players pushing together, can also solve this flat room. Strictly requiring the tether needs level geometry (e.g., a gap or ledge P1 can't reach).

---

## 5. Co-op setup

- **Co-op only:** exactly 2 players on one shared screen. Players **pass through each other** (no collision, no stacking), but can push the same object together.
- **Abilities:** P1 has Kinetic Burst (press). P2 has Wind Tether (hold). Both use the same **Ability** button on their own controls.
- **Controls**, assigned when the room starts:

  | Situation | Player 1 | Player 2 |
  |---|---|---|
  | No gamepads | Keyboard left: A/D move, W jump, LShift dash, F ability | Keyboard right: ←/→, ↑ jump, RShift dash, RCtrl or / ability |
  | 1 gamepad | Full keyboard: WASD/arrows, Space, LShift, F | Gamepad |
  | 2 gamepads | Gamepad 1 | Gamepad 2 |
  | Gamepad layout | Left stick, A/South jump, RB dash, X/West ability | (same) |

- **Not supported yet:** plugging in a controller mid-game.

---

## 6. Known simplifications and open issues

**Simplifications (deliberate; documented in code comments):**
1. Walking, dashing and launching set velocity or use fixed angles for game feel. Forces are used only for pushing blocks.
2. Recoil is scaled to 15% of Newton's 3rd law.
3. Static friction = kinetic friction (Unity 2D limitation).
4. Float's fall-speed cap stands in for air drag.
5. Plate weight = Σmg, not measured normal force.
6. World gravity is 3× Earth. On-screen numbers (if added) will use g = 29.43, e.g. "a 2 kg block weighs 58.9 N". The team should decide how to present this to players (for example "this world has stronger gravity").

7. Wind Tether applies no reaction force to P2 (player assumed braced).

**Resolved (verified in saved files, 2026-11-17):** Push Force 12 N is on `Player_Base` with no Player2 override; world gravity −29.43; all Gravity Scales 1; Pushable Layers = PhysicsObject only; plate threshold 45 N; the block material is renamed `Block_LowFriction` (μ = 1/3, combine Minimum); the plate no longer triggers for sliding objects.

**Open issues:**
- The flat room doesn't *force* the push-and-pull solution (see 4.3).
- `PlayerMovement.TryLaunch()` is currently unused, kept for future spring pads and cannons.
- Some keyboards can't register many simultaneous keys, which may drop inputs on the shared keyboard.
- Kinetic Bloom's design is not yet decided.

---

## 7. Validation checklist (expected vs. observed)

Fill in the last column during play-testing.

| # | Test | Expected | Observed |
|---|---|---|---|
| 1 | Drop the player and the block from the same height | Land at the same time | |
| 2 | Jump height | ~2.5 tiles | |
| 3 | Hold jump while falling | Slow drift (~1.6 tiles in 1 s), then normal fall | |
| 4 | Dash on the ground | ~3 tiles, straight, then 0.5 s cooldown | |
| 5 | One player walks into the 2 kg block | Block doesn't move | |
| 6 | Both players walk into the block | Block moves slowly | |
| 7 | Kinetic Burst on the block | Slides ~3 tiles; P1 nudged back | |
| 8 | Set block mass to 1, burst | ~2× the speed, ~4× the slide distance (d ∝ v²) | |
| 9 | Burst with P2 overlapping or next to P1 | P2 unaffected (not launched, not pushed) | |
| 10 | P2 holds Ability facing the block within 4 m | Tether line appears; block slowly accelerates toward P2 | |
| 11 | Release Ability mid-pull | Line disappears; block slides a little and stops | |
| 12 | Keep pulling until the block is ~1 m from P2 | Pull pauses; block doesn't shove P2 | |
| 13 | Hold Ability facing away from the block | No tether | |
| 14 | Hold Ability with the block > 4 m away | No tether | |
| 15 | Tether with P1 between P2 and the block | Tether works (players don't block line of sight) | |
| 16 | Tether toward P1 alone (no block) | Nothing targeted | |
| 17 | Burst the block so it slides across the plate without stopping | Plate stays yellow, door stays closed | |
| 18 | Block settles on the plate | Plate green (≈ 58.9 N), door opens | |
| 19 | One player standing still on the plate | ≈ 29.4 N, plate stays yellow | |
| 20 | Both players standing still on the plate | ≈ 58.9 N, plate green | |
| 21 | Block on plate + player standing on block | ≈ 88.3 N | |
| 22 | Full puzzle: burst, then tether onto the plate, both exit | "Room complete!" logged once | |
| 23 | Only one player in the open doorway | Room not complete | |
| 24 | Keyboard only / 1 gamepad / 2 gamepads | Controls assigned as in Section 5 | |
