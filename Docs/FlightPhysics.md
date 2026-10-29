# Flight Physics and Progression Plan

How the rocket flies, how parts differ, how fins are placed and how parts unlock. This replaces the old 1D model (`a = T / m - g`, no drag, no rotation).

## Goals

- Flight feels like a real (toy) rocket: thrust pushes, air drags, the rocket rotates when it is badly balanced.
- Hard to master. A badly built rocket flips, curves or tumbles. Players learn by experimenting.
- Same rocket + same random seed = same flight, so results can be tested and later checked on the server.

## Scale and units

Real SI units. The rocket is about 30 cm tall, like a real model rocket.

| Thing | Unit | Tier 1 ballpark |
|---|---|---|
| Mass | kg | whole rocket 0.05 - 0.1 kg |
| Thrust | N | 2 - 5 N |
| Fuel | kg | 0.005 - 0.015 kg per tank |
| Burn rate | kg/s | about 1 s of burn per tank |
| Drag area, side area | m^2 (area x coefficient) | 0.0002 - 0.003 |
| Altitude | m | pure tier 1 rocket reaches about 100 m |

Sanity check without drag: 0.08 kg, 4 N for 1 s gives 40 m/s at burnout, about 20 m up, then coasts about 80 m more. About 100 m.

## The flight model (2D)

The rocket moves in the plane facing the camera: left/right (x), up/down (y) and one rotation (angle from vertical). Thrusters are always on the centre line.

### State

- Position `x, y` (y = altitude), velocity `vx, vy`.
- Angle `theta` (0 = pointing straight up) and angular velocity `omega`.
- Fuel left in each tank.

### Rocket shape (read once at launch)

The flight simulator turns every placed part into a `FlightPart`:

- `position`: where the part sits, relative to the rocket root (x sideways, y up), taken from the scene.
- `mass`, `dragArea`, `sideArea`.
- Tanks: `fuel`. Thrusters: `thrust`, `fuelPerSecond`.

So where the player places a part is exactly what the physics uses.

### Each step (fixed `dt = 0.01 s`)

1. **Fuel.** If any fuel is left, thrusters burn `fuelPerSecond` in total, taken from all tanks evenly.
2. **Mass and centre of mass.** `m = sum(mass + fuel)`. `CoM = sum(m_i * position_i) / m`. It moves as fuel burns.
3. **Inertia** (how hard it is to rotate). `I = sum(m_i * distance_i^2)` from the centre of mass, plus a small minimum.
4. **Air.** `vAir = velocity - wind`. Rocket axis `a = (-sin theta, cos theta)`, sideways `n = (cos theta, sin theta)`.
5. **Forces on each part**, applied at the part's position:
   - Drag: `-0.5 * rho * |vAir| * vAir * dragArea` (against the motion).
   - Side force: `-0.5 * rho * |vAir| * (vAir . n) * sideArea * n` (pushes the part back in line with the air flow).
6. **Thrust** `T * a`, applied on the centre line at the thruster. If the centre of mass is off the centre line (for example fins only on one side), thrust turns the rocket.
7. **Gravity** `(0, -m * g)` at the centre of mass (no turning).
8. **Torque** from every force: `r.x * F.y - r.y * F.x`, where `r` is the force point minus the centre of mass. Plus a small rotation damping `-k * omega`.
9. **Move** (semi-implicit Euler): `v += F / m * dt`, `p += v * dt`, `omega += torque / I * dt`, `theta += omega * dt`.

Constants: `g = 9.81`, `rho = 1.225` (air density), rotation damping `k`, all tunable.

### Why this gives stability

When the rocket tilts, the air hits it from the side. Side forces on parts **below** the centre of mass turn the nose back into the wind (stable). Side forces **above** it turn it further away (unstable). Fins have a big side area, so fins low on the rocket make it stable and fins high up make it flip. This is how real rockets work, and it comes out of the formulas without special rules.

### Wind gusts

A sideways wind that changes over time: the sum of 2 or 3 slow sine waves with random strength and phase, from a `System.Random` seeded at launch. A stable rocket corrects itself; an unstable one gets pushed into a tumble. The seed is saved with the flight so it can be replayed.

### Launch rail

For the first `railLength` metres (about 1 m) the rocket can only move straight up, like the launch rod real model rockets use. This gives it speed before the fins start working.

### Start, end and score

- On the pad, if thrust is less than weight the rocket does not move.
- The flight ends when the rocket comes back down to `y = 0`, or when it runs out of time.
- Score = highest `y` reached.

### Speed-up and skip

Because the sim is plain C# and fast, skipping just runs all remaining steps at once. Speeding up runs several steps per frame.

## Code layout

| Class | Kind | Job |
|---|---|---|
| `FlightPart` | plain C# | One part's numbers and position for the sim. |
| `RocketSim` | plain C# | Holds the state and has `Step(dt)`. No Unity scene code, so tests and the server can use it. |
| `FlightSimulator` | MonoBehaviour | Reads the parts from the scene into `FlightPart`s at launch, steps `RocketSim`, moves and rotates the rocket on screen, reports altitude, speed and fuel to `GameState`. |
| `RocketStats` | plain C# | Build-screen totals (mass, thrust, fuel, thrust to weight), updated to the new units. |

Visuals: on screen position = start + `(x, y) * visualScale`, rotation = `theta` around the camera's forward axis. The camera follows x and y.

## Part data

All parts (`PartData`): `mass`, `dragArea`, `sideArea`, `price`, `tier`, `unlockAltitude`.

- Thruster: `thrust`, `fuelPerSecond`.
- Fuel tank: `fuelCapacity` in kg (the old `massPerFuel` goes away).

### Variants (same tier = same quality, different trade-offs)

| Part | Trade-off | Tier 1 examples |
|---|---|---|
| Nose | drag vs mass | Flat cap (light, draggy), Dome (middle), Cone (low drag, heavier) |
| Fin | side area vs drag vs mass | Stubby (light, weak), Wide (strong, draggy), Swept (middle) |
| Tank | fuel vs dry mass vs length | Thin long tank, short fat tank |
| Thruster | thrust vs burn time | Punchy (strong, short), Sipper (weak, long) |
| Body | mass vs length (more room for fins) | Short, long |

Higher tiers raise all numbers at once (real improvements).

## Fins: one at a time, 10 height steps

- No more fin pairs or mirror logic. Each fin is placed alone, on the left or right.
- Bodies and tanks get a `SnapLine` on each side instead of a side snap point: two end points covering the part's full height.
- Each line is split into **10 evenly spaced steps**. While dragging, the fin follows the cursor and snaps to the closest free step. The same steps on both sides make it easy to match left and right fins.
- One fin per step. Lopsided placements are allowed (and teach the player why they curve).
- Saved in the rocket design as: the part it is on, side, step index (0 - 9).

## Unlocks by altitude

- `unlockAltitude` on each part. A part appears in the store once the best altitude reaches it, then the player buys it with credits.
- "Available" (reached the altitude) is different from "owned" (bought, what `PlayerSession.IsUnlocked` means today).
- Example: 2 of each part type at 0 m, one more of each at 30 m, 50 m and 75 m, the first tier 2 parts from about 150 m.
- The next locked part can show greyed out with "Reach 50 m" so players have a goal.

## Build order

0. **Done.** Removed the part-scaling code. Kept "body first" and the greyed-out cards.
1. **Part data.** Add `dragArea`, `sideArea`, `tier`, `unlockAltitude`. Fuel in kg. Set tier 1 numbers on the existing assets.
2. **`RocketSim` + EditMode tests:**
   - same seed gives the same altitude,
   - a balanced tier 1 rocket reaches about 100 m,
   - fins at the bottom fly higher than fins at the top,
   - fins on one side only make the rocket drift sideways,
   - not enough thrust stays on the pad.
3. **`FlightSimulator` uses `RocketSim`.** Position and rotation on screen, camera follows sideways, random seed per launch, speed-up and skip.
4. **Fin snap lines.** Remove mirror logic, add `SnapLine` with 10 steps, one fin per step.
5. **Store unlocks by altitude** and tier labels on the cards.
6. **Later:** centre of mass and centre of pressure markers with a stability readout while building, tank fill slider, tier 2 parts.
