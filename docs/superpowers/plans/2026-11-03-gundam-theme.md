# Gundam Invaders Theme Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reskin the working Space Invaders 3D game as "Gundam Invaders": hand-built, animated pixel-art mobile suits in orbit, with an intro cutscene, explosions and a Gundam-style HUD. Gameplay rules stay the same.

**Architecture:** All art is character grids in C#. At runtime they become point-filtered `Texture2D`s and are shown through `SpriteRenderer`s. A `PixelSkin` component hides an object's existing primitive meshes and draws its sprite, plus animated child parts (thrusters, mono-eye, muzzle flash). Gameplay scripts each call `PixelSkin.Apply` in `Start`, so no prefab or scene file is edited. New code lives in `Assets/Theme/`. It compiles into `Assembly-CSharp` like the rest of the project.

**Tech Stack:** Unity 6000.6.0f1, URP (Universal Renderer), Input System (`Keyboard.current`), TextMeshPro, uGUI.

**Spec:** The approved concept canvas at https://claude.ai/artifact/L9vqSsk7taNPuvzmffULbB. Sprite grids and palettes live in each board's `renderVals()`. Read them with the Artifact tool (`action: "read"`, `path: "project/<Board>.dc.html"`). The boards are Main, Intro, Enemies, Animation, Stages and AddOns.

## Global Constraints

- Only create or edit C# scripts under `Assets/`. Never hand-edit `.unity`, `.prefab` or `.meta` files. Never touch `Library/`, `Temp/` or `Logs/`. (User rule.)
- No new asset files: no `.asmdef`, images, materials or fonts. Unity may generate `.meta` files and folder metas itself.
- Gameplay rules are unchanged: same hit counts, speeds, lives and win/loss conditions. ("Same rules as today, new skin on top", from the AddOns board.)
- Inspector fields follow `[Header("Inscribed")]` / `[Header("Dynamic")]`. Comment non-obvious logic, because a grader reads the code.
- All textures use `FilterMode.Point` and `TextureWrapMode.Clamp`. Every distinct sprite is built once and cached.
- Palette hex values are copied exactly from the canvas. Key values: void `#0D0B18`, night Earth `#1B2142`, sunrise `#F4A340`, Federation blue `#3E5FB8`, Gundam white `#E9E6EF`, mono-eye pink `#FF6FAE`, Zaku green `#5E8C4A`.
- In play, the Gundam is drawn from behind (the `backRows` + `backPatches` grid). The front view (`frontRows` + `frontPatches`) appears only in the intro. Enemies face the player.
- Fleet layout: the top row (grid row 0, y = 10) is Rick Dom. The two lower rows are Zaku II.
- Explosion: 6 frames at 100 ms each, using the frame grids from the Animation board. Hit stop is 50 ms per kill. Screen shake is 0.08 world units for 120 ms when the ship is hit.
- The ship's hit blink lasts exactly `Spaceship.invulnerableSeconds`, currently 1 s.
- The intro is about 7 s, using the beats and timings on the Intro board. It plays once per application run, Enter skips it, and an R restart does not replay it.
- HUD copy: the alien counter becomes `ZEON FORCES: N`. Lives become `ARMOR ■■□`, filled for remaining and hollow for lost.
- Verification: there's no automated test assembly, because tests need an `.asmdef`, which the user's rules forbid, and gameplay code lives in `Assembly-CSharp`. The executable check is an Editor menu validator (Task 1), plus a Play-mode checklist per task. The user runs each checklist and confirms it before the next task starts.
- After each task: stop. Give the Play-mode steps, any Editor wiring, and a one-line commit message. Commit and push only when the user asks, and never with a co-author trailer.

## Review Focus

1. **Enter pressed to skip the intro.** It should only skip. It must not also start the round on the same frame (Task 7).
2. **R pressed during a 50 ms hit stop.** The reloaded scene should run at normal speed, with `Time.timeScale` back to 1 (Task 4).
3. **A dart that visibly overlaps a mech's body.** It should register a hit, so collider size has to match the sprite and not the old 0.9 sphere (Task 3).
4. **Resizing the Game view or browser window mid-round.** Earth and the sunrise stay anchored to the bottom-right corner (Task 5).
5. **The ship pressed against a screen edge during a shake.** It should stay clamped without jittering, so the clamp must not follow the shaking camera (Task 4).

---

### Task 1: Pixel-art core, sprite library and validator

**Files:**
- Create: `Assets/Theme/PixelArt.cs`
- Create: `Assets/Theme/SpriteLibrary.cs`
- Create: `Assets/Editor/SpriteValidator.cs`

**Interfaces:**
- Produces:
  - `public sealed class SpriteDef { public string Id; public string[] Rows; public Dictionary<char, Color32> Palette; public float PixelsPerUnit; }`. The grid's row 0 is the top. `.` is transparent.
  - `PixelArt.Hex(string hex) -> Color32`
  - `PixelArt.Patch(string[] rows, int x, int y, string[] patch) -> string[]`. This returns a new array. `.` in `patch` leaves the pixel unchanged. It is the same behavior as the canvas `sprite()` patches.
  - `PixelArt.Rasterize(string[] rows, IReadOnlyDictionary<char, Color32> palette, out int width, out int height) -> Color32[]`. The output is bottom-up for `Texture2D.SetPixels32`. It throws `ArgumentException` on ragged rows or on a character missing from the palette.
  - `PixelArt.GetSprite(SpriteDef def) -> Sprite` (cached by `Id`, pivot 0.5/0.5)
  - `PixelArt.GetSilhouette(SpriteDef def) -> Sprite` (same shape, every opaque pixel white, cached)
  - `PixelArt.Solid(Color32 c) -> Sprite` (1×1, ppu 1, cached per color)
  - `PixelArt.Circle(int diameterPx, Color32 c, float ppu) -> Sprite` (cached by arguments)
  - `SpriteLibrary` static fields, all `SpriteDef`: `GundamBack`, `GundamFront`, `Pilot`, `Zaku`, `RickDom`, `CharZaku`, `Haro`, `BeamShot`, `ZakuTracer`, `SpriteDef[] Explosion` (length 6). It also has `IEnumerable<SpriteDef> All`.
  - Pixels per unit: Gundam 14, Zaku, Rick Dom and Char's Zaku 12, Pilot 16, Haro 8, explosion 6, projectiles 16.

- [ ] **Step 1: Write the validator first.** In `Assets/Editor/SpriteValidator.cs`, add a `[MenuItem("Tools/Space Invaders/Validate Sprites")]` that calls `Rasterize` on every `SpriteLibrary.All` entry. It also asserts the following:
  - Every grid has equal-width rows, and every character is in the palette.
  - `GundamBack` is 28 × 26 and `Zaku` is 20 × 19.
  - `RickDom` is 20 × 15 and `CharZaku` is 20 × 20.
  - `Explosion.Length == 6`, and every explosion frame is 8 × 8.
  - `PixelArt.Patch(new[]{"...."}, 1, 0, new[]{"A.B"})` returns `".A.B"`. Rasterizing `{"A.","..."}` throws `ArgumentException`.
  - On success it logs `Sprite check passed: <n> sprites`. Otherwise it calls `Debug.LogError` with each failing `Id` and the reason.
- [ ] **Step 2: Check that it fails before the library exists.** The Console shows a compile error naming `SpriteLibrary`.
- [ ] **Step 3: Implement `PixelArt`** with the signatures above.
- [ ] **Step 4: Implement `SpriteLibrary`.** Copy the grids, patches and hex values verbatim from these canvas boards:
  - Gundam back and front, plus the pilot: from the Intro board (`backRows`/`backPatches`, `frontRows`/`frontPatches`, `pilot`).
  - Zaku, Rick Dom with its bazooka patch, and Char's Zaku: from the Enemies board.
  - Haro: from the AddOns board.
  - The explosion `frames` and their palette: from the Animation board.
  - In the front view, `E` uses `#2A2540` (eyes off). The glow is a separate child (Task 7).
  - The two projectile sprites are new, with these palettes:
    - `BeamShot` is 3 × 10: core `#FFD1E6` in the middle column, edges `#FF6FAE`.
    - `ZakuTracer` is 2 × 6 in `#F2C230`.
- [ ] **Step 5: Verify.** Click into the Editor, then run **Tools → Space Invaders → Validate Sprites**. Expected: `Sprite check passed: 15 sprites` and no red Console lines.
- [ ] **Step 6: Commit message:** `Add pixel-art rasterizer, Gundam/Zeon sprite library and sprite validator`

### Task 2: PixelSkin component and the Gundam

**Files:**
- Create: `Assets/Theme/PixelSkin.cs` (also defines `SkinDef`)
- Modify: `Assets/Theme/SpriteLibrary.cs` (adds `Skins`)
- Modify: `Assets/Spaceship.cs` (`Start`, the movement block, the firing block)

**Interfaces:**
- Consumes: `SpriteDef`, `PixelArt.GetSprite`, `PixelArt.GetSilhouette`, `PixelArt.Solid`
- Produces:
  - `public sealed class SkinDef { public SpriteDef Body; public RectInt Eye; public int EyeSweepPx; public Color32 EyeColor; public RectInt[] Flames; public float BobAmplitude; public float BobPeriod; public Vector2Int Muzzle; }`. Rects are in sprite pixels, measured from the top-left. An empty `Eye` (w = 0) means no eye. `Muzzle.x < 0` means no muzzle.
  - `SpriteLibrary.Skins.Gundam`. Flames are at (9,26,3,4) and (16,26,3,4), the muzzle at (24,0), bob 0.08 u over 2.4 s, and there is no eye.
  - `PixelSkin.Apply(GameObject target, SkinDef skin) -> PixelSkin`
  - Instance members: `void Fire()`, `void SetLean(float direction)` (-1…1), `void Flash(float seconds)`, `void Blink(float seconds)`, `float EyeSpeed { get; set; }` (default 1), `Vector2 BodySizeWorld { get; }`

- [ ] **Step 1: Implement `PixelSkin.Apply`.** It does the following:
  - Disables every child `MeshRenderer` of the target. Colliders stay.
  - Adds a child `Skin` with a `SpriteRenderer`. It compensates for the parent's `lossyScale` so the world size equals pixels ÷ PPU.
  - Places that child at z = -0.1.
- [ ] **Step 2: Implement the animated parts in `Update`.**
  - Bob: the `Skin` child moves along a sine with the set amplitude and period.
  - Flames: each one is a child `Solid(#F4A340)` scaled to its rect, with a narrower `#FFF1C9` core. Every 0.08 s, y-scale alternates between 0.6 and 1.2, with the pivot at the top edge.
  - Eye: a `Solid(EyeColor)` child ping-pongs across `EyeSweepPx` over 1.4 s, divided by `EyeSpeed`.
  - `SetLean`: rotation eases toward `-8° × direction` at about 10 per second.
  - `Fire`: shows a `#FFF1C9` 3 × 3 px muzzle sprite for 60 ms and pushes the body down 0.05 u, recovering over 100 ms.
  - `Flash`: swaps in the silhouette sprite for the given duration.
  - `Blink`: toggles visibility every 0.08 s for the given duration, then makes the body visible again.
- [ ] **Step 3: Hook up `Spaceship`.**
  - `Start` calls `PixelSkin.Apply(gameObject, SpriteLibrary.Skins.Gundam)` and caches the result.
  - The movement block calls `skin.SetLean(direction)` every frame, including 0.
  - The firing block calls `skin.Fire()` after it creates the dart.
- [ ] **Step 4: Play-mode check.**
  - The cylinders are gone. A white, blue and red Gundam seen from behind hovers gently at the bottom, about 2 units wide.
  - The foot flames flicker, and the ship leans into left and right movement.
  - Space shows a muzzle flash at the rifle tip with a small recoil.
  - The edge clamp still stops the visible sprite on screen.
  - The Console shows no errors.
- [ ] **Step 5: Commit message:** `Add PixelSkin and render the player as an animated pixel-art RX-78-2`

### Task 3: Zeon fleet and projectile skins

**Files:**
- Modify: `Assets/Theme/SpriteLibrary.cs` (adds `Skins.Zaku`, `Skins.RickDom`, `Skins.BeamShot`, `Skins.ZakuTracer`)
- Modify: `Assets/Theme/PixelSkin.cs` (collider fitting)
- Modify: `Assets/AlienFleet.cs` (`Start` spawn loop, `FixedUpdate`, new properties)
- Modify: `Assets/Alien.cs`, `Assets/Dart.cs`, `Assets/AlienBullet.cs` (each `Start`)

**Interfaces:**
- Consumes: `PixelSkin.Apply`, `PixelSkin.EyeSpeed`, `PixelSkin.BodySizeWorld`
- Produces:
  - Skin values:
    - `Skins.Zaku`: eye (7,3,2,1) sweeping 8 px, flames (6,19,2,3) and (12,19,2,3).
    - `Skins.RickDom`: eye (6,2,2,1) sweeping 12 px, flames (3,15,3,3) and (14,15,3,3).
    - Both: eye color `#FF6FAE`, bob 0.06 u over 2.4 s with a per-instance random phase.
  - `AlienFleet.SpeedRatio => Mathf.Abs(speed) / baseSpeed` (float)
  - `AlienFleet.Velocity`: a `Vector3` in units per second. It's `(speed / Time.fixedDeltaTime, 0, 0)` while moving sideways and `(0, -|speed| / Time.fixedDeltaTime, 0)` while moving down.
  - `public void Alien.Skin(SkinDef def)`. The fleet calls it right after `Instantiate`: Rick Dom for grid row 0, Zaku otherwise.

- [ ] **Step 1: Fit colliders in `PixelSkin.Apply`.** If the target has a `SphereCollider`, set its radius so the world radius is `0.4 × min(BodySizeWorld.x, BodySizeWorld.y)`. This divides out `lossyScale`. It covers Review Focus 3.
- [ ] **Step 2: Skin the aliens.** `AlienFleet.Start` calls `alien.GetComponent<Alien>().Skin(row == 0 ? SpriteLibrary.Skins.RickDom : SpriteLibrary.Skins.Zaku)`. `Alien.FixedUpdate` sets `skin.EyeSpeed = fleet.SpeedRatio`.
- [ ] **Step 3: Skin the projectiles.** `Dart.Start` applies `Skins.BeamShot`. `AlienBullet.Start` applies `Skins.ZakuTracer`. Neither has an eye, flames or bob.
- [ ] **Step 4: Play-mode check.**
  - The top row shows purple Rick Doms and the two rows below show green Zakus. All of them hover slightly out of sync, with flickering thrusters and pink mono-eyes that sweep.
  - Eye sweep visibly speeds up as aliens die.
  - Darts are pink beams and alien bullets are yellow tracers.
  - A dart that clips a Zaku's shoulder kills it.
  - The counter, speed-up, win and loss all behave as before.
- [ ] **Step 5: Commit message:** `Skin the Zeon fleet and projectiles with animated pixel-art sprites`

### Task 4: Explosions and hit feedback

**Files:**
- Create: `Assets/Theme/Explosion.cs`
- Create: `Assets/Theme/GameFeel.cs`
- Modify: `Assets/Alien.cs` (`OnCollisionEnter`), `Assets/AlienBullet.cs` (`OnCollisionEnter`), `Assets/Spaceship.cs` (`Start`, `OnCollisionEnter`, `HorizontalLimit` caller)

**Interfaces:**
- Consumes: `SpriteLibrary.Explosion`, `PixelSkin.Flash`, `PixelSkin.Blink`, `AlienFleet.Velocity`
- Produces:
  - `Explosion.Spawn(Vector3 position, Vector3 drift)`. It plays the 6 frames at 0.1 s each using unscaled time. Eight `Solid` debris pixels (`#6E6A8A`, `#F4A340`) fly out at random, add `drift`, fade over 0.6 s, and then the object destroys itself.
  - `GameFeel.HitStop(float seconds)`
  - `GameFeel.Shake(float seconds, float amplitude)`
  - `GameFeel` is a self-creating singleton. It restores `Time.timeScale = 1` and the camera's original position in `OnDestroy` and on `SceneManager.sceneLoaded`.

- [ ] **Step 1: Implement `Explosion` and `GameFeel`.** `HitStop` sets `timeScale` to 0, waits with `WaitForSecondsRealtime`, then restores it. `Shake` offsets `Camera.main` from its cached rest position each frame and returns it to that position afterwards.
- [ ] **Step 2: Wire the alien kill.** In `Alien.OnCollisionEnter`, spawn `Explosion.Spawn(transform.position, fleet.Velocity)` and call `GameFeel.HitStop(0.05f)` before destroying.
- [ ] **Step 3: Wire the bullet-versus-dart case.** In `AlienBullet.OnCollisionEnter`, when the other object is a dart, spawn an explosion with zero drift and no hit stop.
- [ ] **Step 4: Wire the ship hit.** When `Spaceship.OnCollisionEnter` passes its grace check, call these:
  - `skin.Flash(0.1f)`
  - `skin.Blink(invulnerableSeconds)`
  - `GameFeel.Shake(0.12f, 0.08f)`
- [ ] **Step 5: Stabilize the clamp.** `Spaceship` caches the camera's x in `Start` and uses that as the clamp centre instead of reading `cam.transform.position.x` every frame. That covers Review Focus 5.
- [ ] **Step 6: Play-mode check.**
  - Every kill freezes briefly, then bursts through yellow, orange and a ring into drifting debris that follows the fleet's direction.
  - Getting hit flashes the Gundam white, shakes the screen and blinks it for 1 s.
  - Hold an arrow against the edge while getting hit: the ship doesn't jitter.
  - Press R right after a kill: the new round runs at normal speed (Review Focus 2).
- [ ] **Step 7: Commit message:** `Add pixel explosions, hit stop, screen shake and hit flash/blink`

### Task 5: Orbital backdrop

**Files:**
- Modify: `Assets/Starfield.cs` (keeps the class name, so the scene component still binds)

**Interfaces:**
- Consumes: `PixelArt.Circle`, `PixelArt.Solid`
- Produces: none for later tasks.

- [ ] **Step 1: Add the background layers** behind the existing stars. Every layer sits behind the gameplay plane at z = 0, with the camera at z = -10.
  - Earth at z = 19: three stacked `Circle` sprites, `#B5503F` 620 px, `#F4A340` 596 px and `#1B2142` 572 px, at ppu 24. They're anchored so the planet's centre sits about 4 units past the bottom-right corner of the view.
  - Sunrise at z = 18.9: a `#FFE3A8` circle at the right edge of the limb, with alpha pulsing between 0.45 and 1 over 1.3 s.
  - Side 7 colony at z = 18, built from `Solid` rects using the sizes and colors on the Stages board, stage 1. It drifts 1 unit left and right over 14 s.
  - Battle flashes at z = 17: four `#FFF1C9` circles that each pop and fade on a 2.6 s cycle, at random positions in the upper half.
  - Near debris at z = 16: four `#4A4458` squares that tumble across the outer 15% of the view only.
- [ ] **Step 2: Handle window resizing.** When `cam.aspect` changes, recompute the anchor positions (Review Focus 4).
- [ ] **Step 3: Play-mode check.**
  - Earth's night side and an orange sunrise rim sit in the bottom-right, with a glow pulsing on the limb.
  - A colony cylinder drifts slowly in the background. Distant flashes pop, and debris tumbles past the edges.
  - Drag the Game view to a different shape: Earth stays in the corner.
  - Gameplay objects always draw in front.
- [ ] **Step 4: Commit message:** `Add orbital backdrop: Earth sunrise, Side 7 colony, battle flashes and debris`

### Task 6: Gundam HUD copy

**Files:**
- Modify: `Assets/AlienCounter.cs` (`SetCount`), `Assets/GameManager.cs` (`Awake`, `UpdateLivesText`, `ReportWin`, `ReportLoss`, `ReportShipHit`), `Assets/AlienFleet.cs` (the loss reason string)

**Interfaces:**
- Produces: `GameManager.SetStartText(string richText)`. Task 7 reuses it.

- [ ] **Step 1: Update the copy.**
  - The counter shows `ZEON FORCES: N`.
  - Lives show `ARMOR ` plus `■` repeated for each remaining life and `□` for each lost life.
  - The start panel's child text, set in `Awake`, reads `<color=#F4A340>GUNDAM INVADERS</color>` then `Press ENTER to launch`.
  - Win: `MISSION COMPLETE`.
  - Losses: `MOBILE SUIT LOST` followed by either `The Zeon fleet broke through` or `The Gundam was destroyed`.
- [ ] **Step 2: Play-mode check.**
  - All the new text appears in place of the old wording.
  - `■` and `□` render, not as empty boxes. If they show up as boxes, check that TMP's dynamic fallback font is enabled.
  - The armor display updates on every hit.
- [ ] **Step 3: Commit message:** `Restyle HUD and end screens with Gundam wording and armor pips`

### Task 7: Intro cutscene

**Files:**
- Create: `Assets/Theme/IntroSequence.cs`
- Modify: `Assets/GameManager.cs` (`Awake`, `Update`, new `OnIntroFinished`)

**Interfaces:**
- Consumes: `SpriteLibrary.GundamFront`, `GundamBack`, `Pilot`, `PixelArt.Solid`, `GameFeel.Shake`
- Produces:
  - `IntroSequence.HasPlayed` (static bool, survives scene reloads)
  - `IntroSequence.IsPlaying` (static bool)
  - `GameManager.OnIntroFinished()` shows the start panel and records `Time.frameCount`.

- [ ] **Step 1: Gate the start panel on the intro.**
  - In `GameManager.Awake`, if `!IntroSequence.HasPlayed`, hide the start panel and `AddComponent<IntroSequence>()`.
  - `Update` ignores Enter while `IntroSequence.IsPlaying`, and on the frame `OnIntroFinished` ran. That covers Review Focus 1.
- [ ] **Step 2: Implement `IntroSequence` as one coroutine.** It builds its own objects at z = -5, sized to cover the camera view, and destroys them when it finishes or is skipped. Each beat's layout comes from the Intro board, and the beats use these timings:
  - 0.0 s: the hangar (`#1E1B2E`, a starry door, a yellow/black hazard stripe), the front-view Gundam at ppu 4, and the pilot floating near its head.
  - 1.5 s: the pilot drifts along a curve to the chest, and the hatch rect at (12,10,4,3) shows `#F4A340`.
  - 3.0 s: the pilot is hidden, the hatch becomes `#3E5FB8`, and `Shake(0.15f, 0.06f)` runs.
  - 3.8 s: a `#C79BFF` visor glow pulses over (12,4,4,1), and a red alert overlay pulses.
  - 4.8 s: the back view appears, with speed lines scrolling left and a TMP banner reading `RX-78-2, LAUNCH!` in `#F2C230`.
  - 6.0 s: everything is cleared.
  - 7.0 s: it finishes.
- [ ] **Step 3: Handle skipping and finishing.** Enter skips at any time. Finishing sets `HasPlayed = true` and calls `GameManager.OnIntroFinished()`.
- [ ] **Step 4: Play-mode check.**
  - Press Play. The intro runs about 7 s, then the start screen appears.
  - Press Enter mid-intro. It skips straight to the start screen, and the round does not start until Enter is pressed again.
  - Lose, then press R. There's no intro the second time.
  - Stop Play and press Play again. The intro plays again.
- [ ] **Step 5: Commit message:** `Add intro cutscene: pilot boards the Gundam and launches from White Base`

### Task 8: Score and Char's Zaku flyover

**Files:**
- Create: `Assets/Theme/CharFlyover.cs`
- Modify: `Assets/GameManager.cs` (score field, `AddScore`), `Assets/Alien.cs` (`points`), `Assets/AlienFleet.cs` (sets points, flyover timer), `Assets/Editor/SceneSetup.cs` (creates and wires `ScoreText`), `Assets/Theme/SpriteLibrary.cs` (`Skins.CharZaku`)

**Interfaces:**
- Consumes: `PixelSkin.Apply`, `Explosion.Spawn`, `SceneSetup` helpers (`CreateText`, `SetIfEmpty`)
- Produces:
  - `GameManager.AddScore(int points)`. The HUD shows `SCORE 000000`, padded to 6 digits, in a new `scoreText` slot.
  - `public int Alien.points`. It's 30 for Rick Dom and 10 for Zaku.
  - `CharFlyover.Spawn(float y, float direction)`

- [ ] **Step 1: Add the score.** `GameManager.AddScore` updates the text. `Alien.OnCollisionEnter` calls `AddScore(points)` only when the other object is a dart, so a ship collision doesn't score. `SceneSetup` creates a `ScoreText` in the bottom-left, at 36 pt with `TopLeft` alignment, and wires it.
- [ ] **Step 2: Add the flyover timer.** While the round is playing, `AlienFleet` spawns a flyover every 20 s, but only once its highest alien is below y = 8.5, at y = 10.3. The direction alternates each time.
- [ ] **Step 3: Implement `CharFlyover`.**
  - The object is on layer `Alien` (`LayerMask.NameToLayer("Alien")`) with a kinematic `Rigidbody` (no gravity) and a `SphereCollider`. The collider is fitted by `PixelSkin` using `Skins.CharZaku`: eye (7,4,2,1), flames (6,20,2,3) and (12,20,2,3).
  - It moves at 18 units per second and trails two ghost sprites at alpha 0.35 and 0.15, spaced 0.6 u apart.
  - It despawns off screen.
  - On a dart hit, it explodes, adds 150 points, and destroys itself and the dart.
  - It is not added to the fleet list, so the win condition is unchanged.
- [ ] **Step 4: Play-mode check.**
  - Run **Tools → Space Invaders → Set Up Scene** once, then check that the score slot is filled.
  - A Zaku kill adds 10 and a Rick Dom kill adds 30.
  - Once the fleet has dropped, a red Zaku with afterimages streaks across the top about every 20 s. Hitting it adds 150.
  - Clearing the fleet still wins, even while Char is on screen.
- [ ] **Step 5: Commit message:** `Add score display and Char's Zaku bonus flyover`

### Task 9: Federation cover blocks

**Files:**
- Create: `Assets/Theme/CoverBlocks.cs` (also defines the marker component `CoverCell`)
- Modify: `Assets/Spaceship.cs` (`Start`), `Assets/Alien.cs` (`OnCollisionEnter`)

**Interfaces:**
- Produces:
  - `CoverBlocks.Build(float y, float[] xs)`
  - `public class CoverCell : MonoBehaviour {}` (a marker only)

- [ ] **Step 1: Build the blocks.** `Spaceship.Start` calls `CoverBlocks.Build(-3.5f, new[]{-12f, -4f, 4f, 12f})`.
  - Each block uses the canvas `coverRows` (8 × 5). Every `C` or `c` cell is its own 0.35 u object with a `Solid` sprite (`#6E7FB0` / `#4A5A8A`), a `BoxCollider` and `CoverCell`.
  - Cells go on layer `Spaceship`. Alien bullets then destroy cells through their existing handler, and darts pass through, because the layer matrix disables Bullet↔Spaceship.
- [ ] **Step 2: Let aliens eat cover.** `Alien.OnCollisionEnter` checks for a `CoverCell` first. If it finds one, it destroys only the cell and returns, so the alien survives and no kill is counted.
- [ ] **Step 3: Play-mode check.**
  - Four blue blocks sit above the ship.
  - Alien tracers chip single cells out, and your beams pass through.
  - Aliens that descend into the blocks erase cells without dying.
  - The counter and win condition are unchanged.
- [ ] **Step 4: Commit message:** `Add destructible Federation cover blocks`

### Task 10: Haro callouts

**Files:**
- Create: `Assets/Theme/HaroCallout.cs`
- Modify: `Assets/GameManager.cs` (`StartGame`, `ReportShipHit`), `Assets/AlienFleet.cs` (`RemoveAlien`), `Assets/Theme/CharFlyover.cs` (`Spawn`)

**Interfaces:**
- Consumes: `SpriteLibrary.Haro`
- Produces: `HaroCallout.Say(string line)`. It's self-creating. The Haro sprite sits in the bottom-right of the viewport at z = -1 and bounces every 0.15 s while talking. A world-space `TextMeshPro` bubble (`#F3ECE2` background sprite, `#141225` text) shows for 2 s, and a new line replaces the current one.

- [ ] **Step 1: Add the lines and their triggers.**
  - Round start: `HARO! ZEON FLEET INBOUND!`
  - Char spawns: `HARO! RED COMET!`
  - Lives reach 1: `HARO! ARMOR CRITICAL!`
  - 5 aliens left: `HARO! ALMOST CLEAR!`
- [ ] **Step 2: Play-mode check.**
  - Each trigger shows its line once, at the right moment, next to a bouncing green Haro.
  - The bubble never covers the ship's lane, and it disappears after 2 s.
- [ ] **Step 3: Commit message:** `Add Haro callouts for key moments`

---

**Not in this plan.** These stretch items from the AddOns board are left out: stages 2 and 3, Big Zam, the Black Tri-Stars dive, Gelgoog, and sound. Rick Dom's 2-hit armor from the roster is also left out, because it would change gameplay rules. Each one would need its own plan.
