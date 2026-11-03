# Space Invaders 3D: Assignment Notes

## Requirements completed

| # | Requirement | Status | Where |
|---|---|---|---|
| 1 | Fix at least 3 code-quality issues | Done (3 documented below, plus 2 smaller fixes) | `Alien.cs`, `AlienFleet.cs`, `AlienCounter.cs`, `Spaceship.cs`, `Dart.cs`, `AlienBullet.cs` |
| 2 | Keep the player ship on screen | Done | `Spaceship.cs`: `HorizontalLimit()` |
| 3 | Aliens speed up as others are destroyed | Done | `AlienFleet.cs`: `UpdateSpeed()` |
| 4 | Game over when aliens advance too far | Done | `AlienFleet.cs` (`gameOverY` check), `GameManager.cs` |
| 5 | Start screen and win/lose end screens with restart | Done (Enter to start, R to restart) | `GameManager.cs` |
| 6 | Lives with an on-screen display | Done (3 lives, 1 s grace period after a hit) | `GameManager.cs`, `Spaceship.cs`, `AlienBullet.cs` |
| 7 | Replace the background | Done (black background, scrolling parallax starfield) | `Starfield.cs` |
| 8 | Rebalance to be harder but beatable | Done (see table below) | Several scripts |

`Assets/Editor/SceneSetup.cs` is an editor-only helper. The menu item **Tools > Space Invaders > Set Up Scene** creates and wires the scene objects for items 4 to 7. It is not part of the game build.

## Code-quality fixes

### 1. Redundant `GetComponent` and another object's list edited directly (`Alien.cs`)

**Before**
```csharp
public AlienFleet fleet;
...
void OnCollisionEnter(Collision coll)
{
    fleet.GetComponent<AlienFleet>().aliens.Remove(gameObject);
    ...
}
```

**What was wrong.** `fleet` is already an `AlienFleet`, so `GetComponent<AlienFleet>()` looked up the object it already had. Each alien also edited the fleet's list directly, so the list had 30 writers. The counter and any future logic had to poll the list to notice changes. A hand-placed alien with no fleet threw a `NullReferenceException`.

**Why it matters.** A lookup on every collision is wasted work. When outside code can change a list, every rule tied to that list has to be enforced by each caller.

**After**
```csharp
// Alien.cs
if (fleet != null) fleet.RemoveAlien(gameObject);

// AlienFleet.cs
public void RemoveAlien(GameObject alien)
{
    aliens.Remove(alien);
    alienCounter.SetCount(aliens.Count);
    UpdateSpeed();                        // requirement 3
    if (aliens.Count == 0) GameManager.ReportWin();
}
```
The fleet now owns its list. Its one removal method also became the hook for the speed-up, the win condition and the counter.

### 2. Two loops per tick, a fragile bounds search, and magic numbers (`AlienFleet.cs`)

**Before**
```csharp
for (int i = 0; i < 10; i++)
    for (int j = 0; j < 3; j++)
        alien.transform.position = new Vector3(2 * i - 12, 10 - 2 * j, 0);
...
foreach (GameObject alien in aliens) { /* move */ }
float left = 0, right = 0;
foreach (GameObject alien in aliens) { /* find left/right */ }
```

**What was wrong.** Every physics tick looped over all aliens twice. `left` and `right` started at 0, so whenever every alien was on one side of x = 0, one of them stayed at 0 instead of the real edge. That only worked because the bounds (±16) sit on either side of 0. If the bounds didn't, for example in a play area shifted right with `leftBound = 2`, `left` would stay at 0 and the fleet would reverse every tick. The grid size and spacing were unexplained literals.

**Why it matters.** The fleet update runs 50 times a second. The bounds bug would show up as soon as anyone changed the grid or the bounds. Values like `2 * i - 12` can't be tuned without reading and changing code.

**After**
```csharp
[SerializeField] private int columns = 10;
[SerializeField] private int rows = 3;
[SerializeField] private float spacing = 2f;
[SerializeField] private Vector2 gridOrigin = new Vector2(-12f, 10f);
...
float left = float.MaxValue, right = float.MinValue, lowest = float.MaxValue;
foreach (GameObject alien in aliens)
{
    // move, then:
    left = Mathf.Min(left, position.x);
    right = Mathf.Max(right, position.x);
    lowest = Mathf.Min(lowest, position.y);   // used by requirement 4
}
```
One pass now moves the aliens and measures the fleet, and the extremes start from the correct values. The grid layout can be tuned in the Inspector.

### 3. The counter label was rebuilt every frame (`AlienCounter.cs`, `AlienFleet.cs`)

**Before**
```csharp
// AlienFleet.Update, every frame
alienCounter.score = aliens.Count;

// AlienCounter.Update, every frame
uiText.text = "Remaining Aliens: " + score.ToString("#,0");
```

**What was wrong.** The count changes at most 30 times per game. This code still built a new string and assigned it to TextMeshPro every frame. TextMeshPro regenerates its text mesh on every assignment, even when the text is the same. The public `score` field also let any script write to the display.

**Why it matters.** The game allocated garbage and rebuilt the mesh every frame for nothing. In larger projects this per-frame UI pattern is a common source of garbage-collection stutter.

**After**
```csharp
// AlienCounter.cs
void Awake() { uiText = GetComponent<TextMeshProUGUI>(); }
public void SetCount(int count)
{
    uiText.text = "Remaining Aliens: " + count.ToString("#,0");
}
```
The fleet calls `SetCount` once at spawn and once per kill. The component is cached in `Awake` so it is ready before the fleet's `Start`.

### Smaller fixes

- **Empty `Start`/`Update` stubs and unused `using` lines removed** from five scripts. Unity calls an empty `Update` on every instance every frame.
- **Duplicated movement code in `Spaceship` merged.** The left and right blocks are now one signed-direction calculation. `lastShot` changed from public to `[SerializeField] private` in `Spaceship` and `Alien`. A null check on `Keyboard.current` was added.

## Rebalance (requirement 8)

| Value | Before | After | Reason |
|---|---|---|---|
| Player shot cooldown | 0.05 s | 0.35 s | The old value allowed near-unlimited spam, so aiming didn't matter |
| Player move speed | 10 u/s | 12 u/s | Keeps the faster bullets dodgeable |
| Alien bullet speed | 5 u/s | 7.5 u/s | Less time to react |
| Fleet starting speed | 5 u/s | 6 u/s | The fleet descends sooner |
| Fleet speed-up per kill | 5% | 4% | The last alien moves about 13 u/s, close to the old 12, so it can still be hit |
| Alien fire chance per tick | 0.002 | 0.0025 | About 3.75 shots/s from a full fleet, up from 3 |
| Survivor fire scaling | none | × √(starting ÷ remaining) | The last alien used to fire about once every 10 s and now fires about 0.7 times per second |

Lives (3) and the game-over line (y = -4.5) were left unchanged. With no kills, the fleet now reaches the line in about 40 seconds instead of about 60. Thirty aliens at one shot per 0.35 s need at least 10.5 seconds of firing, so the game can be won with roughly a 50% hit rate.

## Controls

- Left and right arrows move the ship. Space fires.
- Enter starts the round. R restarts after a win or a loss.
