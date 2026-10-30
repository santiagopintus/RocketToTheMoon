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

### Each step (fixed `dt = 0.005 s`)

1. **Fuel.** If any fuel is left, thrusters burn `fuelPerSecond` in total, taken from all tanks evenly.
2. **Mass and centre of mass.** `m = sum(mass + fuel)`. `CoM = sum(m_i * position_i) / m`. It moves as fuel burns.
3. **Inertia** (how hard it is to rotate). `I = sum(m_i * distance_i^2 + m_i * length_i^2 / 12)` from the centre of mass.
4. **Air.** Each part meets the air at `vAir = velocity + spin x r - wind` (the spin part is what calms wobbling down). Rocket axis `a = (-sin theta, cos theta)`, sideways `n = (cos theta, sin theta)`.
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

For the first `railLength` metres (about 1 m) the rocket can only slide along the rail, like the launch rod real model rockets use. This gives it speed before the fins start working. The rail leans `railTilt` degrees (5) to a random side from the seed, so the rocket flies an arc, noses over at the top and lands away from the pad (about 15 m for the starter).

### Start, end and score

- On the pad, if thrust is less than weight the rocket does not move.
- Altitude is the height of the centre of mass above where it started.
- The flight ends (credits and result popup) when the rocket falls back to 70% of its highest altitude, hits the ground, never lifts off, or runs out of time (120 s). After the popup the sim keeps running so the rocket falls to the ground.
- The ground is `padHeight` (0.2 m) below the pad top. Hitting it faster than `crashSpeed` (8 m/s) hides the rocket, plays the explosion and shakes the camera.
- Score = highest altitude reached. Credits are paid only for a new record: `creditsPerMetre` (6) for every metre above the old record.
- On screen the rocket moves at real scale (`visualScale` = 1), so it moves as fast as it spins.
- A rocket without a nose gets extra drag (`noNoseDrag`) at its flat top.

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

Higher tiers raise all numbers at once (real improvements). For thrusters that means more push per gram of fuel (thrust / fuelPerSecond), not just more thrust: a stronger thruster that burns fuel as fast is no better.

### Current numbers (tier 1 starter: small thruster, normal tank, short body, normal nose, small fins)

| Part | Mass (g) | Drag area | Side area | Other | Unlock |
|---|---|---|---|---|---|
| Body Short / Long | 60 / 90 | 0.0003 / 0.0005 | 0.0006 / 0.001 | | 0 m |
| Nose Normal / Sharp | 30 / 40 | 0.0018 / 0.001 | 0.0008 / 0.0006 | | 0 m |
| Fin Small / Large | 8 / 16 | 0.0002 / 0.0005 | 0.004 / 0.008 | | 0 m |
| Tank Normal / Large | 20 / 35 | 0.0002 / 0.0003 | 0.0003 / 0.0004 | fuel 40 g / 80 g | 0 m / 50 m |
| Thruster Small (T1) | 25 | 0.0003 | 0.0003 | 12 N, 40 g/s | 0 m |
| Thruster Medium (T2) | 40 | 0.0003 | 0.0003 | 24 N, 50 g/s | 150 m |
| Thruster Large (T2) | 60 | 0.0004 | 0.0004 | 45 N, 70 g/s | 300 m |

Tier 3 (the `_T3` models; `_1` is cheapest, `_3` most expensive, each with its own trade-off):

| Part | Mass (g) | Drag area | Side area | Other | Price | Unlock |
|---|---|---|---|---|---|---|
| Body Mid / Tower | 80 / 120 | 0.0006 / 0.0008 | 0.0028 / 0.0042 | Tower: more room for fins | 400 / 750 | 400 / 550 m |
| Nose Dome / Cone / Spike | 30 / 45 / 70 | 0.0015 / 0.001 / 0.0006 | 0.0008 / 0.0014 / 0.003 | Spike: big side area up top | 250 / 450 / 700 | 400 / 450 / 600 m |
| Fin Swept | 20 | 0.0004 | 0.012 | | 350 | 400 m |
| Tank Compact / Tall / Giant | 40 / 80 / 150 | 0.0004 / 0.0005 / 0.0016 | 0.0008 / 0.0014 / 0.003 | fuel 130 / 260 / 550 g | 600 / 1000 / 1800 | 400 / 500 / 700 m |
| Thruster Sipper / Booster / Titan | 70 / 110 / 170 | 0.0006 / 0.0008 / 0.0011 | same as drag | 45 N 50 g/s / 80 N 95 g/s / 150 N 170 g/s | 1400 / 2200 / 3200 | 400 / 550 / 750 m |

Thrusters: the Sipper is the most fuel-efficient but weak, the Titan pushes hardest but needs a lot of fuel.

The `BalanceReport` EditMode test prints the average altitude of several rockets (5 winds each) to the test results. Run it after changing numbers. Last run: starter with fins low on the body about 85 m, fins in the middle 37 m, fins at the top 31 m, one fin 16 m, sharp nose 94 m, large tank 170 m, T2 with large tank 203 m, T2 with two large tanks 330 m, Thruster Large with two large tanks 373 m, T3 Sipper with two compact tanks 665 - 752 m (dome / cone / spike), Booster with two tall tanks 939 m, Titan with two giant tanks 1195 m.

## Fins: one at a time, 10 height steps

- No more fin pairs or mirror logic. Each fin is placed alone, on the left or right.
- The existing left and right snap points of bodies and tanks act as lines: same x as the point, covering the part's full mesh height (worked out at runtime, no prefab changes).
- Each line is split into **10 evenly spaced steps**. While dragging, the fin follows the cursor and snaps to the closest free step. The same steps on both sides make it easy to match left and right fins.
- One fin per step. Lopsided placements are allowed (and teach the player why they curve).
- Saved in the rocket design as: the part it is on, side, step index (0 - 9).

## Unlocks by altitude

- `unlockAltitude` on each part. A part appears in the store once the best altitude reaches it, then the player buys it with credits.
- "Available" (reached the altitude) is different from "owned" (bought, what `PlayerSession.IsUnlocked` means today).
- The best altitude is saved in `PlayerSession.BestAltitude` (PlayerPrefs for now, the cloud profile in Sprint 3).
- Goal: 2 of each part type at 0 m, one more of each at 30 m, 50 m and 75 m, the first tier 2 parts from about 150 m. Today only the existing 11 parts exist (see the table above).
- Locked parts show greyed out with "REACH 50 m" so players have a goal. The store sorts cards by unlock altitude, then price, and shows the tier next to the name.

## Build order

0. **Done.** Removed the part-scaling code. Kept "body first" and the greyed-out cards.
1. **Done.** Part data: `dragArea`, `sideArea`, `tier`, `unlockAltitude`, fuel in kg, real numbers on the 11 assets.
2. **Done.** `RocketSim` + EditMode tests (same seed same flight, starter about 100 m, low fins beat high fins, no wind flies straight, one fin drifts, too heavy stays on the pad, balance report).
3. **Done, except speed-up and skip.** `FlightSimulator` uses `RocketSim`: position and rotation on screen, camera follows sideways, random seed per launch. `RocketSim.RunToEnd()` is ready for a skip button.
4. **Done.** Fins: no more pairs. Left and right snap points are lines with 10 steps (`SnapPoint.IsLine`, `StepPosition`), worked out from the part's mesh height, one fin per step.
5. **Done.** Store unlocks by best altitude and tier labels on the cards.
6. **Next:** speed-up and skip buttons, centre of mass and centre of pressure markers with a stability readout while building, more tier 1 variants (new models), tank fill slider.
