# Shotgun Climber — Design Spec

**Date:** 2026-07-12
**Project:** OneButtonSubmission (Unity 6.3, `6000.3.19f1`, Built-in Render Pipeline)
**Context:** Game jam. Hard constraint: **only one button may be pressed** during play.

---

## 1. Concept

A punishing 2.5D vertical climber in the spirit of *Getting Over It*. The player's only
tool is a shotgun that **constantly sweeps in a full circle**. The single button **fires**
it, and the recoil blasts the body in the opposite direction. Reach the summit of one
handcrafted mountain. Mistime a shot, or run out of shells, and you tumble back down —
**no checkpoints, no reset**. The pain of losing progress *is* the game.

### Design pillars
- **Timing tension** — the barrel is always sweeping; you fire at the right angle or waste the shot.
- **Recoil mastery** — movement is entirely emergent from well-aimed impulses.
- **The pain of falling** — punishment, not forgiveness. Getting Over It DNA.

---

## 2. Technical foundation

**3D physics, gameplay locked to a 2D plane.** The environment assets are 3D FBX
(rocks, trees, grass), so the world is genuinely 3D and rendered with a perspective camera
for depth/parallax, while all gameplay happens in the X-Y plane.

- Player is a 3D `Rigidbody`.
- Constraints: **freeze Z position**, **freeze X & Y rotation**. The body may only translate
  in X/Y and tumble around the Z axis.
- 3D meshes (rocks etc.) act as colliders directly.
- Rejected alternative: 2D physics (`Rigidbody2D`) — would require flattening all 3D assets
  to sprites/2D colliders and discards the imported art. Not pursued.

---

## 3. Player physics model

- **Grey-box body** (placeholder art): capsule = body, thin cylinder = arm, box = shotgun.
  All Unity primitives. Real art swapped in later.
- **Single `Rigidbody`** with the plane-lock constraints above.
- **Self-righting spring**: a PD-controller torque about the Z axis that pulls the body back
  to upright. Strong enough to normally stand, but **can be overwhelmed** by hard landings and
  steep slopes — so the body still slides off ledges and loses progress. This is how
  "upright-ish humanoid" and "true Getting Over It punishment" coexist.
- **Gravity**: tunable, starting around **-20** (punchier than Unity default -9.81).

---

## 4. Gun & shooting (the heartbeat)

- Gun pivots at the hand and **auto-rotates a full 360° at constant angular speed**
  (starting **~120°/sec**), independent of the body's orientation.
- **Fire** (button press): apply a **strong impulse** to the body directed *opposite* the
  barrel's forward direction (aim the barrel down → launch up). Add a small kick-torque for juice.
- **Pump cooldown** (~**0.6s**): cannot fire again until reloaded; a visible pump/reload
  indicator communicates readiness.
- **Limited ammo**: start with **~6 shells**; each shot consumes one.
  **Ammo pickups** placed on ledges refill shells. Empty = stranded (must reach a pickup).

All numbers above are **starting points to tune** during the feel-tuning milestone.

---

## 5. Camera

- **Perspective** camera, side-on to the play plane.
- **Smoothly follows** the player (SmoothDamp) with a slight upward look-ahead.
- Depth from 3D scenery behind the play plane provides the 2.5D parallax look.

---

## 6. Level & win/lose

- **One handcrafted vertical mountain**, grey-boxed from primitives + the imported rock FBX,
  with trees/grass as backdrop.
- Ledges to land on and re-launch from; **ammo pickups** placed along the route.
- **Win**: a trigger volume at the summit → win screen.
- **Lose**: no death state. Falling simply costs progress and you re-climb. No reset button —
  Getting Over It style.

---

## 7. Input — the one-button constraint

- A **single `Shoot` action** (Space / left-mouse / gamepad South) via the existing
  `InputSystem_Actions` asset.
- Gun rotation, reload, and everything else are **automatic**. One button drives the entire game.

---

## 8. Architecture (scripts)

Small, single-purpose MonoBehaviours:

| Script | Responsibility |
|---|---|
| `PlayerBody` | Rigidbody setup, plane-lock, self-righting spring, `ApplyRecoil(direction, force)` |
| `GunController` | Constant gun rotation, reads fire input, enforces cooldown, calls `ApplyRecoil` |
| `AmmoSystem` | Shell count, consumption, refill API |
| `AmmoPickup` | Trigger volume that refills ammo on contact |
| `CameraFollow` | Smooth follow with look-ahead |
| `GameManager` | Win state, summit trigger handling, UI wiring |
| UI | Ammo count, reload/pump indicator, win screen |

---

## 9. Build order (feel before content)

1. Plane-locked capsule that falls and rests on ground.
2. Camera smooth-follow.
3. Gun pivot auto-rotation + visual.
4. **Recoil impulse + pump cooldown — playtest the core feel here.**
5. Ammo system + pickups + UI.
6. Self-righting-spring & tumble tuning.
7. Grey-box mountain level with ledges + ammo placement.
8. Summit win trigger + win/lose UI.
9. Placeholder polish (muzzle flash particle, sound hooks).

**Implementation note:** planning is done in Opus; C# implementation of individual tasks can
be dispatched to Sonnet subagents per the plan.

---

## 10. Out of scope (YAGNI for the jam)

- Procedural generation, multiple levels, enemies.
- Speedrun timer / score (could be a fast-follow, not in v1).
- Final character/gun art and rigging (placeholders only for v1).
- Save/load, settings menus beyond a start + win screen.
