# Shotgun Climber Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a punishing 2.5D one-button vertical climber where a constantly-sweeping shotgun's recoil is your only means of movement.

**Architecture:** All game logic lives in pure, unit-tested C# classes under `Assets/Scripts/Core` (ammo, cooldown gate, gun sweep, recoil vector, upright PD spring). Thin MonoBehaviour wrappers under `Assets/Scripts/Components` bind that logic to Unity physics/input. A single `GameBootstrap` MonoBehaviour constructs the entire scene (player, camera, grey-box level, pickups, HUD) at runtime so the game is reproducible from pressing Play — no fragile hand-authored scene wiring.

**Tech Stack:** Unity 6000.3.19f1, Built-in Render Pipeline, Input System 1.19, Unity Test Framework (NUnit, EditMode), C#.

## Global Constraints

- Unity Editor version: **6000.3.19f1** (installed at `C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe`).
- Render pipeline: **Built-in** (do NOT add URP).
- Input: **exactly one button** is used during play (`Shoot`). Everything else is automatic. This is a hard jam constraint — never bind a second gameplay action.
- All runtime scripts live under `Assets/Scripts/` in namespace `OneButtonSubmission.*` and compile under the `OneButtonSubmission` asmdef.
- All tests are **EditMode** tests under `Assets/Tests/EditMode/`.
- Gameplay is 3D physics **locked to the X-Y plane**: freeze Z position, freeze X & Y rotation.
- Commit messages are plain — **no `Co-Authored-By` or attribution trailers**.
- Starting tunable values (all serialized, tuned during playtests): gravity **-20**, gun sweep **120°/s**, recoil force **10**, pump cooldown **0.6s**, start/max shells **6**, pickup refill **3**.

### Running EditMode tests (used by every logic task)

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Unity.exe" \
  -batchmode -projectPath "D:/work/OneButtonSubmission" \
  -runTests -testPlatform EditMode \
  -testResults "D:/work/OneButtonSubmission/TestResults.xml" \
  -logFile - 2>&1 | tail -40
```

Exit code `0` = all tests passed; non-zero = failure. Inspect `TestResults.xml` for per-test detail. The Editor must not be open on this project simultaneously (batchmode needs the project lock). Playtest gates are performed by the human opening the project in the Editor and pressing Play.

---

### Task 1: Project scaffolding + AmmoSystem

Sets up assembly definitions (the test pipeline) and the first pure logic class.

**Files:**
- Create: `Assets/Scripts/OneButtonSubmission.asmdef`
- Create: `Assets/Scripts/Core/AmmoSystem.cs`
- Create: `Assets/Tests/EditMode/OneButtonSubmission.Tests.EditMode.asmdef`
- Create: `Assets/Tests/EditMode/AmmoSystemTests.cs`

**Interfaces:**
- Consumes: nothing (first task).
- Produces:
  - `OneButtonSubmission.Core.AmmoSystem` with ctor `AmmoSystem(int max, int start)`, props `int Max`, `int Current`, `bool IsEmpty`, methods `bool TryConsume()`, `void Refill(int amount)`.

- [ ] **Step 1: Create the runtime assembly definition**

`Assets/Scripts/OneButtonSubmission.asmdef`:
```json
{
    "name": "OneButtonSubmission",
    "rootNamespace": "OneButtonSubmission",
    "references": ["Unity.InputSystem"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "autoReferenced": true
}
```

- [ ] **Step 2: Create the test assembly definition**

`Assets/Tests/EditMode/OneButtonSubmission.Tests.EditMode.asmdef`:
```json
{
    "name": "OneButtonSubmission.Tests.EditMode",
    "references": ["OneButtonSubmission", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "precompiledReferences": ["nunit.framework.dll"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "autoReferenced": false,
    "overrideReferences": true
}
```

- [ ] **Step 3: Write the failing test**

`Assets/Tests/EditMode/AmmoSystemTests.cs`:
```csharp
using NUnit.Framework;
using OneButtonSubmission.Core;

public class AmmoSystemTests
{
    [Test]
    public void Starts_At_Given_Count_Clamped_To_Max()
    {
        var ammo = new AmmoSystem(max: 6, start: 6);
        Assert.AreEqual(6, ammo.Current);
        Assert.AreEqual(6, ammo.Max);
        Assert.IsFalse(ammo.IsEmpty);
    }

    [Test]
    public void TryConsume_Decrements_Until_Empty_Then_Fails()
    {
        var ammo = new AmmoSystem(6, 2);
        Assert.IsTrue(ammo.TryConsume());
        Assert.IsTrue(ammo.TryConsume());
        Assert.IsFalse(ammo.TryConsume());
        Assert.AreEqual(0, ammo.Current);
        Assert.IsTrue(ammo.IsEmpty);
    }

    [Test]
    public void Refill_Adds_But_Caps_At_Max()
    {
        var ammo = new AmmoSystem(6, 0);
        ammo.Refill(3);
        Assert.AreEqual(3, ammo.Current);
        ammo.Refill(10);
        Assert.AreEqual(6, ammo.Current);
    }

    [Test]
    public void Refill_Ignores_NonPositive()
    {
        var ammo = new AmmoSystem(6, 2);
        ammo.Refill(0);
        ammo.Refill(-5);
        Assert.AreEqual(2, ammo.Current);
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

Run the EditMode test command from Global Constraints.
Expected: FAIL / compile error — `AmmoSystem` does not exist yet.

- [ ] **Step 5: Write minimal implementation**

`Assets/Scripts/Core/AmmoSystem.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Finite shotgun shells. Pure logic, no Unity scene dependency.
    public class AmmoSystem
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsEmpty => Current <= 0;

        public AmmoSystem(int max, int start)
        {
            Max = Mathf.Max(0, max);
            Current = Mathf.Clamp(start, 0, Max);
        }

        public bool TryConsume()
        {
            if (Current <= 0) return false;
            Current--;
            return true;
        }

        public void Refill(int amount)
        {
            if (amount <= 0) return;
            Current = Mathf.Min(Current + amount, Max);
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run the EditMode test command. Expected: PASS (4 passed).

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts Assets/Tests
git commit -m "feat: project scaffolding + AmmoSystem with tests"
```

---

### Task 2: FireGate (pump cooldown)

**Files:**
- Create: `Assets/Scripts/Core/FireGate.cs`
- Create: `Assets/Tests/EditMode/FireGateTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `OneButtonSubmission.Core.FireGate` with ctor `FireGate(float cooldown)`, methods `bool CanFire(float now)`, `void RegisterFire(float now)`, `float ReloadProgress(float now)` (returns 0..1).

- [ ] **Step 1: Write the failing test**

`Assets/Tests/EditMode/FireGateTests.cs`:
```csharp
using NUnit.Framework;
using OneButtonSubmission.Core;

public class FireGateTests
{
    [Test]
    public void Can_Fire_Before_First_Shot()
    {
        var gate = new FireGate(0.6f);
        Assert.IsTrue(gate.CanFire(0f));
        Assert.AreEqual(1f, gate.ReloadProgress(0f));
    }

    [Test]
    public void Cannot_Fire_During_Cooldown()
    {
        var gate = new FireGate(0.6f);
        gate.RegisterFire(0f);
        Assert.IsFalse(gate.CanFire(0.3f));
        Assert.AreEqual(0.5f, gate.ReloadProgress(0.3f), 1e-4f);
    }

    [Test]
    public void Can_Fire_After_Cooldown_Elapses()
    {
        var gate = new FireGate(0.6f);
        gate.RegisterFire(0f);
        Assert.IsTrue(gate.CanFire(0.6f));
        Assert.IsTrue(gate.CanFire(1.0f));
        Assert.AreEqual(1f, gate.ReloadProgress(0.6f));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run the EditMode test command. Expected: FAIL — `FireGate` does not exist.

- [ ] **Step 3: Write minimal implementation**

`Assets/Scripts/Core/FireGate.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Enforces the pump-action reload window between shots.
    public class FireGate
    {
        readonly float cooldown;
        float lastFireTime;
        bool hasFired;

        public FireGate(float cooldown)
        {
            this.cooldown = Mathf.Max(0f, cooldown);
            hasFired = false;
            lastFireTime = 0f;
        }

        public bool CanFire(float now)
            => !hasFired || (now - lastFireTime) >= cooldown;

        public void RegisterFire(float now)
        {
            hasFired = true;
            lastFireTime = now;
        }

        public float ReloadProgress(float now)
        {
            if (!hasFired || cooldown <= 0f) return 1f;
            return Mathf.Clamp01((now - lastFireTime) / cooldown);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run the EditMode test command. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/FireGate.cs Assets/Tests/EditMode/FireGateTests.cs
git commit -m "feat: FireGate pump cooldown with tests"
```

---

### Task 3: GunRotator (constant sweep)

**Files:**
- Create: `Assets/Scripts/Core/GunRotator.cs`
- Create: `Assets/Tests/EditMode/GunRotatorTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `OneButtonSubmission.Core.GunRotator` static class, method `static float Advance(float angleDeg, float speedDegPerSec, float dt)` returning the new angle wrapped into `[0, 360)`.

- [ ] **Step 1: Write the failing test**

`Assets/Tests/EditMode/GunRotatorTests.cs`:
```csharp
using NUnit.Framework;
using OneButtonSubmission.Core;

public class GunRotatorTests
{
    [Test]
    public void Advances_By_Speed_Times_Dt()
    {
        float a = GunRotator.Advance(0f, 120f, 0.5f);
        Assert.AreEqual(60f, a, 1e-4f);
    }

    [Test]
    public void Wraps_Past_360()
    {
        float a = GunRotator.Advance(350f, 120f, 0.1f); // 350 + 12 = 362 -> 2
        Assert.AreEqual(2f, a, 1e-4f);
    }

    [Test]
    public void Stays_In_Zero_To_360_Range()
    {
        float a = GunRotator.Advance(359f, 120f, 1f); // 359 + 120 = 479 -> 119
        Assert.GreaterOrEqual(a, 0f);
        Assert.Less(a, 360f);
        Assert.AreEqual(119f, a, 1e-4f);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run the EditMode test command. Expected: FAIL — `GunRotator` does not exist.

- [ ] **Step 3: Write minimal implementation**

`Assets/Scripts/Core/GunRotator.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Advances the gun's sweep angle at a constant rate, wrapped to [0,360).
    public static class GunRotator
    {
        public static float Advance(float angleDeg, float speedDegPerSec, float dt)
            => Mathf.Repeat(angleDeg + speedDegPerSec * dt, 360f);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run the EditMode test command. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/GunRotator.cs Assets/Tests/EditMode/GunRotatorTests.cs
git commit -m "feat: GunRotator constant sweep with tests"
```

---

### Task 4: RecoilCalculator (recoil vector)

**Files:**
- Create: `Assets/Scripts/Core/RecoilCalculator.cs`
- Create: `Assets/Tests/EditMode/RecoilCalculatorTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `OneButtonSubmission.Core.RecoilCalculator` static class, method `static Vector2 Impulse(float barrelAngleDeg, float force)`. The barrel points along `barrelAngleDeg` in the X-Y plane (0° = +X, 90° = +Y); the returned impulse points **opposite** the barrel.

- [ ] **Step 1: Write the failing test**

`Assets/Tests/EditMode/RecoilCalculatorTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using OneButtonSubmission.Core;

public class RecoilCalculatorTests
{
    [Test]
    public void Barrel_Down_Launches_Up()
    {
        // barrel at -90 (pointing down) -> impulse points up (+Y)
        Vector2 impulse = RecoilCalculator.Impulse(-90f, 10f);
        Assert.AreEqual(0f, impulse.x, 1e-4f);
        Assert.AreEqual(10f, impulse.y, 1e-4f);
    }

    [Test]
    public void Barrel_Right_Pushes_Left()
    {
        // barrel at 0 (pointing +X) -> impulse points -X
        Vector2 impulse = RecoilCalculator.Impulse(0f, 10f);
        Assert.AreEqual(-10f, impulse.x, 1e-4f);
        Assert.AreEqual(0f, impulse.y, 1e-4f);
    }

    [Test]
    public void Magnitude_Equals_Force()
    {
        Vector2 impulse = RecoilCalculator.Impulse(37f, 8f);
        Assert.AreEqual(8f, impulse.magnitude, 1e-4f);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run the EditMode test command. Expected: FAIL — `RecoilCalculator` does not exist.

- [ ] **Step 3: Write minimal implementation**

`Assets/Scripts/Core/RecoilCalculator.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Turns a barrel angle + force into a recoil impulse opposite the barrel.
    public static class RecoilCalculator
    {
        public static Vector2 Impulse(float barrelAngleDeg, float force)
        {
            float rad = barrelAngleDeg * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            return -forward * force;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run the EditMode test command. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/RecoilCalculator.cs Assets/Tests/EditMode/RecoilCalculatorTests.cs
git commit -m "feat: RecoilCalculator recoil vector with tests"
```

---

### Task 5: UprightController (self-righting PD spring)

**Files:**
- Create: `Assets/Scripts/Core/UprightController.cs`
- Create: `Assets/Tests/EditMode/UprightControllerTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `OneButtonSubmission.Core.UprightController` static class, method `static float ComputeTorque(float currentAngleDeg, float angularVelDeg, float kp, float kd, float maxTorque)`. Returns a Z-torque that drives lean toward 0°, clamped to `±maxTorque` so hard impacts can overwhelm it.

- [ ] **Step 1: Write the failing test**

`Assets/Tests/EditMode/UprightControllerTests.cs`:
```csharp
using NUnit.Framework;
using OneButtonSubmission.Core;

public class UprightControllerTests
{
    [Test]
    public void Zero_Lean_Zero_Velocity_Gives_Zero_Torque()
    {
        float t = UprightController.ComputeTorque(0f, 0f, 10f, 1f, 100f);
        Assert.AreEqual(0f, t, 1e-4f);
    }

    [Test]
    public void Positive_Lean_Produces_Negative_Corrective_Torque()
    {
        float t = UprightController.ComputeTorque(1f, 0f, 10f, 1f, 100f);
        Assert.AreEqual(-10f, t, 1e-4f);
    }

    [Test]
    public void Torque_Is_Clamped_To_Max()
    {
        // huge lean would demand -6000, clamp to -400
        float t = UprightController.ComputeTorque(30f, 0f, 200f, 30f, 400f);
        Assert.AreEqual(-400f, t, 1e-4f);
    }

    [Test]
    public void Damping_Opposes_Angular_Velocity()
    {
        // no lean, spinning positively -> negative (damping) torque
        float t = UprightController.ComputeTorque(0f, 5f, 10f, 2f, 100f);
        Assert.AreEqual(-10f, t, 1e-4f);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run the EditMode test command. Expected: FAIL — `UprightController` does not exist.

- [ ] **Step 3: Write minimal implementation**

`Assets/Scripts/Core/UprightController.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// PD controller that keeps the body upright but can be overwhelmed on hard hits.
    public static class UprightController
    {
        public static float ComputeTorque(float currentAngleDeg, float angularVelDeg,
            float kp, float kd, float maxTorque)
        {
            float lean = Mathf.DeltaAngle(0f, currentAngleDeg); // normalize to [-180,180]
            float raw = -(kp * lean + kd * angularVelDeg);
            return Mathf.Clamp(raw, -maxTorque, maxTorque);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run the EditMode test command. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/UprightController.cs Assets/Tests/EditMode/UprightControllerTests.cs
git commit -m "feat: UprightController self-righting spring with tests"
```

---

### Task 6: PlayerBody + AmmoSystemBehaviour + GameBootstrap (fall & rest)

First on-screen milestone: a plane-locked capsule that falls onto ground and self-rights. Verified by playtest (physics feel is not unit-testable).

**Files:**
- Create: `Assets/Scripts/Components/PlayerBody.cs`
- Create: `Assets/Scripts/Components/AmmoSystemBehaviour.cs`
- Create: `Assets/Scripts/Bootstrap/GameBootstrap.cs`

**Interfaces:**
- Consumes: `UprightController.ComputeTorque(...)` (Task 5).
- Produces:
  - `OneButtonSubmission.Components.PlayerBody` MonoBehaviour: `[RequireComponent(typeof(Rigidbody))]`; serialized `float uprightKp, uprightKd, uprightMaxTorque`; method `void ApplyRecoil(Vector2 impulse)`.
  - `OneButtonSubmission.Components.AmmoSystemBehaviour` MonoBehaviour: serialized `int maxShells, startShells`; property `AmmoSystem System { get; }` (built in `Awake`).
  - `OneButtonSubmission.Components.GameBootstrap` MonoBehaviour: builds the scene in `Awake`; serialized `float gravityY = -20f`; exposes `public PlayerBody Player { get; private set; }`.

- [ ] **Step 1: Write PlayerBody**

`Assets/Scripts/Components/PlayerBody.cs`:
```csharp
using UnityEngine;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// The player's physical body: plane-locked rigidbody with a self-righting spring.
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerBody : MonoBehaviour
    {
        [Header("Self-righting spring")]
        public float uprightKp = 200f;
        public float uprightKd = 30f;
        public float uprightMaxTorque = 400f;

        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        void FixedUpdate()
        {
            float leanDeg = transform.eulerAngles.z;
            float angVelDeg = rb.angularVelocity.z * Mathf.Rad2Deg;
            float torque = UprightController.ComputeTorque(
                leanDeg, angVelDeg, uprightKp, uprightKd, uprightMaxTorque);
            rb.AddTorque(0f, 0f, torque * Mathf.Deg2Rad, ForceMode.Acceleration);
        }

        public void ApplyRecoil(Vector2 impulse)
        {
            rb.AddForce(new Vector3(impulse.x, impulse.y, 0f), ForceMode.Impulse);
        }
    }
}
```

- [ ] **Step 2: Write AmmoSystemBehaviour**

`Assets/Scripts/Components/AmmoSystemBehaviour.cs`:
```csharp
using UnityEngine;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// Scene-facing wrapper around the pure AmmoSystem.
    public class AmmoSystemBehaviour : MonoBehaviour
    {
        public int maxShells = 6;
        public int startShells = 6;

        public AmmoSystem System { get; private set; }

        void Awake()
        {
            System = new AmmoSystem(maxShells, startShells);
        }
    }
}
```

- [ ] **Step 3: Write GameBootstrap (player + ground only)**

`Assets/Scripts/Bootstrap/GameBootstrap.cs`:
```csharp
using UnityEngine;
using OneButtonSubmission.Components;

namespace OneButtonSubmission.Bootstrap
{
    /// Builds the whole playable scene at runtime so pressing Play is reproducible.
    /// Extended in later tasks (camera, gun, pickups, level, HUD).
    public class GameBootstrap : MonoBehaviour
    {
        [Header("World")]
        public float gravityY = -20f;

        public PlayerBody Player { get; private set; }

        void Awake()
        {
            Physics.gravity = new Vector3(0f, gravityY, 0f);
            Player = BuildPlayer(new Vector3(0f, 2f, 0f));
            BuildGround(new Vector3(0f, -0.5f, 0f), new Vector3(50f, 1f, 4f));
        }

        PlayerBody BuildPlayer(Vector3 pos)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Player";
            body.transform.position = pos;

            var rb = body.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.angularDamping = 1.5f;

            return body.AddComponent<PlayerBody>();
        }

        GameObject BuildGround(Vector3 pos, Vector3 size)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = pos;
            ground.transform.localScale = size;
            return ground;
        }
    }
}
```

- [ ] **Step 4: Wire the bootstrap into the scene**

Open `Assets/Scenes/SampleScene.unity` in the Unity Editor. Create an empty GameObject named `Bootstrap`, add the `GameBootstrap` component to it, and save the scene. (This is the only manual scene edit in the whole project; everything else is built by code.)

- [ ] **Step 5: Playtest — fall & rest**

Press Play. Verify:
- The capsule spawns above the ground, falls, and comes to rest on it.
- It settles roughly upright (self-righting spring working), not lying flat.
- It does not drift in Z (plane lock working).

If it falls too slowly, lower `gravityY` (e.g. -25). If it jitters/spins, lower `uprightKp` or raise `uprightKd` on the Player at runtime and note good values.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Components Assets/Scripts/Bootstrap Assets/Scenes
git commit -m "feat: plane-locked player body + bootstrap (fall and rest)"
```

---

### Task 7: CameraFollow

**Files:**
- Create: `Assets/Scripts/Components/CameraFollow.cs`
- Modify: `Assets/Scripts/Bootstrap/GameBootstrap.cs` (add camera setup)

**Interfaces:**
- Consumes: `GameBootstrap.Player` (Task 6).
- Produces:
  - `OneButtonSubmission.Components.CameraFollow` MonoBehaviour: serialized `Transform target`, `float smoothTime`, `Vector3 offset`.

- [ ] **Step 1: Write CameraFollow**

`Assets/Scripts/Components/CameraFollow.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Smoothly follows the player in the X-Y plane with an upward look-ahead offset.
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.25f;
        public Vector3 offset = new Vector3(0f, 1.5f, -12f);

        Vector3 velocity;

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = new Vector3(target.position.x, target.position.y, 0f) + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
```

- [ ] **Step 2: Add camera wiring to GameBootstrap**

In `Assets/Scripts/Bootstrap/GameBootstrap.cs`, add a field and a build call. Add after the `gravityY` field:
```csharp
        [Header("Camera")]
        public float cameraSmoothTime = 0.25f;
        public Vector3 cameraOffset = new Vector3(0f, 1.5f, -12f);
```
Add at the end of `Awake()` (after ground is built):
```csharp
            BuildCamera(Player.transform);
```
Add this method to the class:
```csharp
        void BuildCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = target;
            follow.smoothTime = cameraSmoothTime;
            follow.offset = cameraOffset;
        }
```

- [ ] **Step 3: Playtest — follow**

Press Play. Verify the camera keeps the falling capsule centered (slightly below center due to look-ahead) and smoothly tracks it. Tune `cameraOffset.z` for zoom and `cameraSmoothTime` for responsiveness.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Components/CameraFollow.cs Assets/Scripts/Bootstrap/GameBootstrap.cs
git commit -m "feat: smooth camera follow"
```

---

### Task 8: GunController — sweep, fire, recoil, cooldown (CORE FEEL)

The single most important playtest gate: does blasting yourself around feel good?

**Files:**
- Create: `Assets/Scripts/Components/GunController.cs`
- Modify: `Assets/Scripts/Bootstrap/GameBootstrap.cs` (build gun pivot + wire controller)

**Interfaces:**
- Consumes: `GunRotator.Advance(...)` (Task 3), `RecoilCalculator.Impulse(...)` (Task 4), `FireGate` (Task 2), `PlayerBody.ApplyRecoil(...)` (Task 6), `AmmoSystemBehaviour.System` (Task 6).
- Produces:
  - `OneButtonSubmission.Components.GunController` MonoBehaviour: serialized `float rotationSpeedDegPerSec, recoilForce, cooldownSeconds`; public fields `Transform gunPivot`, `PlayerBody playerBody`, `AmmoSystemBehaviour ammo`; read-only `float ReloadProgress`, `float CurrentAngle`.

- [ ] **Step 1: Write GunController**

`Assets/Scripts/Components/GunController.cs`:
```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// The one-button heart: sweeps the gun, and on the single Shoot button
    /// consumes a shell and blasts the body opposite the barrel (if reloaded).
    public class GunController : MonoBehaviour
    {
        [Header("Sweep")]
        public float rotationSpeedDegPerSec = 120f;

        [Header("Recoil")]
        public float recoilForce = 10f;
        public float cooldownSeconds = 0.6f;

        public Transform gunPivot;
        public PlayerBody playerBody;
        public AmmoSystemBehaviour ammo;

        float angleDeg;
        FireGate fireGate;
        InputAction fireAction;

        public float CurrentAngle => angleDeg;
        public float ReloadProgress => fireGate == null ? 1f : fireGate.ReloadProgress(Time.time);

        void Awake()
        {
            fireGate = new FireGate(cooldownSeconds);

            // THE one button. Only action bound in the entire game.
            fireAction = new InputAction("Shoot", InputActionType.Button);
            fireAction.AddBinding("<Keyboard>/space");
            fireAction.AddBinding("<Mouse>/leftButton");
            fireAction.AddBinding("<Gamepad>/buttonSouth");
            fireAction.performed += OnFire;
        }

        void OnEnable() => fireAction?.Enable();
        void OnDisable() => fireAction?.Disable();

        void Update()
        {
            angleDeg = GunRotator.Advance(angleDeg, rotationSpeedDegPerSec, Time.deltaTime);
            if (gunPivot != null)
                gunPivot.rotation = Quaternion.Euler(0f, 0f, angleDeg); // world-space sweep
        }

        void OnFire(InputAction.CallbackContext ctx)
        {
            float now = Time.time;
            if (!fireGate.CanFire(now)) return;
            if (ammo != null && !ammo.System.TryConsume()) return;

            Vector2 impulse = RecoilCalculator.Impulse(angleDeg, recoilForce);
            playerBody.ApplyRecoil(impulse);
            fireGate.RegisterFire(now);
        }
    }
}
```

- [ ] **Step 2: Add gun + ammo wiring to GameBootstrap**

In `Assets/Scripts/Bootstrap/GameBootstrap.cs`, add serialized fields after the camera block:
```csharp
        [Header("Gun")]
        public float gunSweepSpeed = 120f;
        public float recoilForce = 10f;
        public float cooldownSeconds = 0.6f;

        [Header("Ammo")]
        public int maxShells = 6;
        public int startShells = 6;
```
Add a property near `Player`:
```csharp
        public GunController Gun { get; private set; }
        public AmmoSystemBehaviour Ammo { get; private set; }
```
In `Awake()`, after `Player = BuildPlayer(...)` and before `BuildCamera(...)`, insert:
```csharp
            Ammo = Player.gameObject.AddComponent<AmmoSystemBehaviour>();
            Ammo.maxShells = maxShells;
            Ammo.startShells = startShells;
            Gun = BuildGun(Player);
```
Add these methods to the class:
```csharp
        GunController BuildGun(PlayerBody body)
        {
            // Pivot lives at the player's "hand"; a barrel box sticks out +X from it.
            var pivot = new GameObject("GunPivot");
            pivot.transform.SetParent(body.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 0.3f, 0f);

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            Destroy(barrel.GetComponent<Collider>()); // visual only
            barrel.transform.SetParent(pivot.transform, false);
            barrel.transform.localScale = new Vector3(1.2f, 0.2f, 0.2f);
            barrel.transform.localPosition = new Vector3(0.7f, 0f, 0f);

            var gun = body.gameObject.AddComponent<GunController>();
            gun.gunPivot = pivot.transform;
            gun.playerBody = body;
            gun.ammo = Ammo;
            gun.rotationSpeedDegPerSec = gunSweepSpeed;
            gun.recoilForce = recoilForce;
            gun.cooldownSeconds = cooldownSeconds;
            return gun;
        }
```

- [ ] **Step 3: Playtest — CORE FEEL**

Press Play. Verify:
- The barrel sweeps continuously around the player at a readable speed.
- Pressing **Space** (or left-click) when the barrel points **down** launches the player **up**.
- After a shot there's a ~0.6s beat where pressing the button does nothing (cooldown).
- Firing sideways shoves the player the opposite way; you can feel yourself being flung around.
- After 6 shots, firing stops working (ammo empty) — you're stranded.

Tune `recoilForce` (launch strength), `gunSweepSpeed` (timing difficulty), `cooldownSeconds` (rhythm) on the Player object at runtime until it feels good. **Record the values you like** — they become the new defaults.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Components/GunController.cs Assets/Scripts/Bootstrap/GameBootstrap.cs
git commit -m "feat: gun sweep, fire, recoil, cooldown (core loop playable)"
```

---

### Task 9: AmmoPickup + HUD

**Files:**
- Create: `Assets/Scripts/Components/AmmoPickup.cs`
- Create: `Assets/Scripts/Components/HudController.cs`
- Modify: `Assets/Scripts/Bootstrap/GameBootstrap.cs` (spawn a pickup + HUD)

**Interfaces:**
- Consumes: `AmmoSystemBehaviour.System.Refill(int)` (Task 6), `AmmoSystemBehaviour.System.Current/Max` (Task 6), `GunController.ReloadProgress` (Task 8).
- Produces:
  - `OneButtonSubmission.Components.AmmoPickup` MonoBehaviour: serialized `int amount`; refills the first `AmmoSystemBehaviour` that enters its trigger, then deactivates.
  - `OneButtonSubmission.Components.HudController` MonoBehaviour: fields `AmmoSystemBehaviour ammo`, `GunController gun`; draws shells + reload bar via IMGUI (`OnGUI`). Also exposes `bool ShowWin` used by Task 10.

- [ ] **Step 1: Write AmmoPickup**

`Assets/Scripts/Components/AmmoPickup.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// A trigger volume that tops up shells when the player touches it, then disappears.
    public class AmmoPickup : MonoBehaviour
    {
        public int amount = 3;

        void OnTriggerEnter(Collider other)
        {
            var ammo = other.GetComponentInParent<AmmoSystemBehaviour>();
            if (ammo == null) return;
            ammo.System.Refill(amount);
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 2: Write HudController**

`Assets/Scripts/Components/HudController.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Minimal IMGUI HUD: shell count, reload bar, and an optional win banner.
    public class HudController : MonoBehaviour
    {
        public AmmoSystemBehaviour ammo;
        public GunController gun;
        public bool ShowWin;

        void OnGUI()
        {
            GUI.skin.label.fontSize = 22;

            if (ammo != null && ammo.System != null)
                GUI.Label(new Rect(20, 20, 300, 30),
                    $"SHELLS  {ammo.System.Current}/{ammo.System.Max}");

            if (gun != null)
            {
                GUI.Box(new Rect(20, 55, 200f, 18f), GUIContent.none);
                GUI.Box(new Rect(20, 55, 200f * Mathf.Clamp01(gun.ReloadProgress), 18f),
                    GUIContent.none);
            }

            if (ShowWin)
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 48,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(0, Screen.height / 2f - 40f, Screen.width, 80f),
                    "SUMMIT REACHED", style);
            }
        }
    }
}
```

- [ ] **Step 3: Add pickup + HUD wiring to GameBootstrap**

In `Assets/Scripts/Bootstrap/GameBootstrap.cs`, add serialized fields:
```csharp
        [Header("Pickups")]
        public int pickupRefill = 3;
```
Add a property near the others:
```csharp
        public HudController Hud { get; private set; }
```
At the end of `Awake()`, after `BuildCamera(...)`, insert:
```csharp
            BuildAmmoPickup(new Vector3(3f, 1f, 0f));
            Hud = BuildHud(Ammo, Gun);
```
Add these methods:
```csharp
        GameObject BuildAmmoPickup(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "AmmoPickup";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.6f;
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            var pickup = go.AddComponent<AmmoPickup>();
            pickup.amount = pickupRefill;
            return go;
        }

        HudController BuildHud(AmmoSystemBehaviour ammo, GunController gun)
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HudController>();
            hud.ammo = ammo;
            hud.gun = gun;
            return hud;
        }
```

- [ ] **Step 4: Playtest — ammo & HUD**

Press Play. Verify:
- Top-left shows `SHELLS n/6`, decrementing by one per shot.
- The reload bar under it empties on fire and refills over the cooldown.
- Blasting into the sphere pickup adds shells (capped at 6) and the sphere vanishes.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Components/AmmoPickup.cs Assets/Scripts/Components/HudController.cs Assets/Scripts/Bootstrap/GameBootstrap.cs
git commit -m "feat: ammo pickups + IMGUI HUD"
```

---

### Task 10: Grey-box mountain + summit win

Builds the actual climb from a list of ledges, plus the win condition.

**Files:**
- Create: `Assets/Scripts/Components/GameManager.cs`
- Create: `Assets/Scripts/Components/SummitTrigger.cs`
- Modify: `Assets/Scripts/Components/HudController.cs` (drive `ShowWin` from GameManager)
- Modify: `Assets/Scripts/Bootstrap/GameBootstrap.cs` (build ledges, pickups along route, summit)

**Interfaces:**
- Consumes: `PlayerBody` (Task 6), `HudController.ShowWin` (Task 9), `AmmoPickup` (Task 9).
- Produces:
  - `OneButtonSubmission.Components.GameManager` MonoBehaviour: `bool HasWon`, `void TriggerWin()`.
  - `OneButtonSubmission.Components.SummitTrigger` MonoBehaviour: field `GameManager gameManager`; calls `TriggerWin()` when the player enters.

- [ ] **Step 1: Write GameManager**

`Assets/Scripts/Components/GameManager.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Tracks the single win condition.
    public class GameManager : MonoBehaviour
    {
        public bool HasWon { get; private set; }

        public void TriggerWin()
        {
            if (HasWon) return;
            HasWon = true;
            Debug.Log("SUMMIT REACHED — YOU WIN");
        }
    }
}
```

- [ ] **Step 2: Write SummitTrigger**

`Assets/Scripts/Components/SummitTrigger.cs`:
```csharp
using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// The flag at the top: entering it wins the game.
    public class SummitTrigger : MonoBehaviour
    {
        public GameManager gameManager;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerBody>() == null) return;
            if (gameManager != null) gameManager.TriggerWin();
        }
    }
}
```

- [ ] **Step 3: Drive the win banner from GameManager**

In `Assets/Scripts/Components/HudController.cs`, add a field near the others:
```csharp
        public GameManager gameManager;
```
Replace the `OnGUI` line `if (ShowWin)` with a version that also honors the manager. Change:
```csharp
            if (ShowWin)
```
to:
```csharp
            if (ShowWin || (gameManager != null && gameManager.HasWon))
```

- [ ] **Step 4: Build the mountain + summit in GameBootstrap**

In `Assets/Scripts/Bootstrap/GameBootstrap.cs`, add serialized fields:
```csharp
        [Header("Level")]
        public Vector2[] ledges = new Vector2[]
        {
            new Vector2(2f, 3f),
            new Vector2(-2f, 6f),
            new Vector2(3f, 9f),
            new Vector2(-1f, 12f),
            new Vector2(2f, 15f),
        };
        public Vector2[] routePickups = new Vector2[]
        {
            new Vector2(-2f, 7f),
            new Vector2(2f, 13f),
        };
        public Vector2 summit = new Vector2(2f, 17f);
```
Add a property:
```csharp
        public GameManager Manager { get; private set; }
```
At the end of `Awake()` (after HUD), insert:
```csharp
            Manager = gameObject.AddComponent<GameManager>();
            BuildLedges();
            foreach (var p in routePickups) BuildAmmoPickup(new Vector3(p.x, p.y, 0f));
            BuildSummit(new Vector3(summit.x, summit.y, 0f), Manager);
            Hud.gameManager = Manager;
```
Add these methods:
```csharp
        void BuildLedges()
        {
            foreach (var l in ledges)
            {
                var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledge.name = "Ledge";
                ledge.transform.position = new Vector3(l.x, l.y, 0f);
                ledge.transform.localScale = new Vector3(2.5f, 0.5f, 4f);
            }
        }

        GameObject BuildSummit(Vector3 pos, GameManager manager)
        {
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Summit";
            flag.transform.position = pos;
            flag.transform.localScale = new Vector3(1.5f, 3f, 4f);
            var col = flag.GetComponent<Collider>();
            col.isTrigger = true;
            var trigger = flag.AddComponent<SummitTrigger>();
            trigger.gameManager = manager;
            return flag;
        }
```

- [ ] **Step 5: Playtest — full climb**

Press Play. Verify:
- A staircase of ledges rises above the start with pickups along the route and a tall summit block at the top.
- You can blast from ledge to ledge; mistiming drops you back down (progress lost).
- Touching the summit prints the win log and shows the `SUMMIT REACHED` banner.
- Adjust the `ledges` / `routePickups` / `summit` arrays on the Bootstrap object until the climb is hard-but-fair. Record final values.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Components/GameManager.cs Assets/Scripts/Components/SummitTrigger.cs Assets/Scripts/Components/HudController.cs Assets/Scripts/Bootstrap/GameBootstrap.cs
git commit -m "feat: grey-box mountain, pickups on route, summit win condition"
```

---

### Task 11: Placeholder polish (muzzle flash + scenery)

Optional jam polish. Adds a muzzle-flash particle burst on each successful shot for game feel. (Dropping in the imported tree/rock FBX as backdrop is left as free-form Editor decoration — no code needed; place them behind the play plane at positive Z with their colliders removed.)

**Files:**
- Modify: `Assets/Scripts/Components/GunController.cs` (fire event hook)
- Modify: `Assets/Scripts/Bootstrap/GameBootstrap.cs` (muzzle flash + scenery)

**Interfaces:**
- Consumes: `GunController.OnFired` event (added here), imported FBX under `Assets/environment/`.
- Produces:
  - `OneButtonSubmission.Components.GunController.OnFired` — `public event System.Action OnFired;` invoked on a successful shot.

- [ ] **Step 1: Add a fire event to GunController**

In `Assets/Scripts/Components/GunController.cs`, add near the fields:
```csharp
        public event System.Action OnFired;
```
In `OnFire`, after `fireGate.RegisterFire(now);`, add:
```csharp
            OnFired?.Invoke();
```

- [ ] **Step 2: Add muzzle flash + scenery to GameBootstrap**

In `Assets/Scripts/Bootstrap/GameBootstrap.cs`, at the end of `Awake()` add:
```csharp
            BuildMuzzleFlash(Gun);
```
Add this method:
```csharp
        void BuildMuzzleFlash(GunController gun)
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.SetParent(gun.gunPivot, false);
            go.transform.localPosition = new Vector3(1.4f, 0f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.15f;
            main.startSpeed = 6f;
            main.startSize = 0.3f;
            main.startColor = new Color(1f, 0.8f, 0.2f);
            var emission = ps.emission;
            emission.enabled = false; // we emit manually on fire
            ps.Stop();

            gun.OnFired += () => ps.Emit(12);
        }
```

- [ ] **Step 3: Playtest — polish**

Press Play. Verify a small yellow spark burst appears at the barrel tip each time you successfully fire (not during cooldown misfires). Confirm nothing regressed in the climb.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Components/GunController.cs Assets/Scripts/Bootstrap/GameBootstrap.cs
git commit -m "feat: muzzle flash particle burst on fire"
```

---

## Notes for the implementer

- **Meta files:** Unity generates `.meta` files for every new asset/folder the first time the Editor imports them. After creating scripts from outside the Editor, open the Editor once to let it generate metas, then include the `.meta` files in the same commit as their assets.
- **Editor vs batchmode lock:** close the Editor before running the batchmode test command, and vice-versa.
- **Feel over content:** Tasks 8 and 10 are the ones that make or break the game. Spend playtest time there; the numbers in this plan are deliberately conservative starting points.
- **One-button rule:** the only input binding in the entire codebase is `GunController.fireAction`. If a future task needs another input, stop — it violates the jam constraint.
```