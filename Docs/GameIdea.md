Rocket Workshop is a single-player rocket-building sandbox with a simple 3D construction system and a lightweight flight simulation. It runs in the browser. Player profiles (unlocked parts, credits, best score, saved rockets) are stored in a cloud database through a backend.

The biggest challenge will be controlling the scope.

## Game concept

[SIMPLE | Space Cartoon Assets | Synty™ Official Store  – Synty Store](https://images.openai.com/static-rsc-4/112nRs7_h83KxbnZQ0zeeBB7BDotDoNVB_JcidndIBqq8DW0QpRejcya2pSgyx4MY-mG2OPRAG67Ab9de5mvFBKs0E-YijdjCNmQqVVyYwBUGLUVxrhV09cZBiPRcWWKi-7EojOdGiYHxTPVzI-gstTKcPBK4KquBY0mjq75Bdk?purpose=inline)

[syntystore.com](https://syntystore.com/products/simple-space-cartoon-assets?utm_source=chatgpt.com)

Working concept: Rocket Workshop

The player designs a rocket using purchased modular parts. They experiment with different configurations, launch their creation, and try to reach the highest possible altitude.

There are no characters to control. Players interact directly with the rocket, its parts, and the construction interface.

Core gameplay loop:

Buy parts

Build rocket

Launch

Measure altitude and earn credits

Improve the rocket and try again

## Development across two sprints

Sprint 2 (Networking) was cancelled. The game is single-player and the work is split in two.

Sprint 1 — Game Framework (current focus)

Build the single-player gameplay loop, then keep making it deeper and more polished.

- 3D construction scene and orbiting camera.
- Parts: command capsule, fuel tank, engine, side connector, and more over time.
- Click to select parts and attach them at predefined snap points.
- Launch button and basic vertical flight physics.
- Maximum altitude, credits, and restart.
- Depth and polish: more parts and upgrades, parts that must be unlocked, better flight feedback and camera, balancing, sound effects and particles.

Deliverable: A player can assemble and launch a rocket, earn credits, and unlock better parts.

Sprint 3 — Cloud Profiles and Saves (after Sprint 1)

Connect the game to a backend that handles all database work.

- Player profile found by name: credits, unlocked parts, best altitude (max score), flights count.
- Create or load the profile when the player enters their name.
- Update the profile after each flight and purchase.
- Save, load, rename and delete rocket designs.
- Store flight history.

Deliverable: Players come back later with the same name and keep their progress.

## Keep the rocket simulation simple

I would avoid realistic orbital mechanics, aerodynamics, staging, structural stress, or complicated fuel systems.

Instead, use a simplified vertical flight model where each part affects a few properties:

| Part                   | Property                    | Gameplay effect            |
| ---------------------- | --------------------------- | -------------------------- |
| Capsule                | Mass                        | Adds weight                |
| Fuel tank              | Fuel capacity and mass      | Extends burn time          |
| Engine                 | Thrust and fuel consumption | Provides acceleration      |
| Optional second engine | More thrust and mass        | Helps lift heavier rockets |

Gravity and fuel consumption determine how high the rocket travels. This is enough to create interesting tradeoffs: adding fuel increases potential flight time but also makes the rocket heavier.

For the initial version, I would keep rockets vertically stacked, use fixed snap points, and simulate only upward motion. You can still show the rocket traveling into the sky with camera tracking and a live altitude indicator.

## Recommended technical architecture

Unity WebGL Client (GitHub Pages)

HTTPS · JSON

Backend API

Validation · Profile logic · CRUD

Cloud MongoDB

Profiles · Rocket designs · Flight history

The game never connects to the database directly. A browser cannot open raw database or TCP connections, and the database credentials must stay on the server. The game sends HTTPS requests to the backend, which validates them and talks to MongoDB. The backend must allow CORS from the GitHub Pages site.

## Modular construction — Parts snap to predefined attachment points, including side-mounted tanks and engines. More creative freedom.

# Rocket Workshop — Game Design Plan

A 3D rocket-building game where the player designs, purchases, assembles, and launches modular rockets to reach the highest possible altitude.

Design priorities: Creative freedom, simple physics, satisfying experimentation, and a manageable development scope.

[Tiny Space Academy on Steam](https://images.openai.com/static-rsc-4/Utzz9sMOk2aXaMPm1Cdz-k7GvtD0LCrDdSGTEy6ISSjGmPSHrBoZZkWoRWCzWnphBLlzdJSgXPN_0N7goe8YTGieY6xogWCJRda-sfQdjbV7G5-Q7X2Y_IC2X6tgQ5lEp5Dso73wgTScVEG5zhLb9sD11Zm-_6USAbW0037pflg?purpose=inline)

[store.steampowered.com](https://store.steampowered.com/app/1519890/Tiny_Space_Academy/?utm_source=chatgpt.com)

## 1. Core gameplay loop

Buy

Build

Launch

Results

Upgrade

Players begin with a small budget and a basic rocket. They can buy additional parts, attach them at predefined connection points, and launch their design.

After each flight, they earn credits based on maximum altitude and use those credits to experiment with improved configurations.

The challenge isn't piloting the rocket. It's engineering a better rocket through trial and error.

## 2. Modular construction system

I recommend a snap-point system with four initial part types.

[Rocket - Download free 3D models | Poly Pizza](https://images.openai.com/static-rsc-4/k73K4vyYT5XScF-FINTpp1AKCH_lYAHfyAuF_D4jrMkW-nit1QBSNaLjftgGqFZxTCterUWkCd29PfrbuBa74RCnWRRgAA8U_npyVABzA7KgIHCxpVp8BUoQZ3vGz4ByqfZ8dhxEVjiPQY5C2OZWhnu9GHJ0QzKcgKFRxfWH-Vo?purpose=inline)

Command capsule

The top of the rocket. Required for launch.

[Tank - Free 3D Model By Quaternius - Poly Pizza](https://images.openai.com/static-rsc-4/PZliAiV45qdwvCvFiJI4EaER8nMxB9mUhbWxL8TCbwDZWsX3a-J2Kiyd1isXWek5V11aUhfidKJvFBqJDoaxA0NVUk_AXuxUBGVbTrVvFcXfdviGOLp6XfiL8CHNkdtYU81mYip_5zFGEh-lp0tvm-vkBByknwgmrR0hxr9y0uY?purpose=inline)

Fuel tank

Stores fuel but increases total mass.

[LowPoly Liquid Fuel Rocket Engine Medium  free VR / AR / low-poly 3D model | CGTrader](https://images.openai.com/static-rsc-4/r2L5hJrKZeQPVUidhXcXLlTdGqBgkL_7h0B7qlYvwC-uGIcigHUlvUfd0IL6LTqE8tFKfuhA6wLVVyXm6jiqQJSgcGyAlo3zYM1UYtDNCBnRU2Kmgb9G0jTJblvGyABvFYKyh8EgN8DNF1sDSb-52FXMpps49sgWKpGSWvJnTHM?purpose=inline)

Engine

Produces thrust while consuming fuel.

[3D model LowPoly Liquid Fuel Rocket Engines Small Pack VR / AR / low-poly | CGTrader](https://images.openai.com/static-rsc-4/BcBGC1mBgF_wtjRW0dqii4mSY28k1jC-SoARBnedun6JFz0q5h36V_RMU-BYz2HS8MqungEf_PjXh2w-ALea2mGYCwvDa13__bgNDrjUaxDOxPAXUGQ_kak5GSn8T7gwKWZ9zNyjUKVdarSzSdCx5z7gbSp6dWe-F2inTGHGzKo?purpose=inline)

Side connector

Allows tanks or engines to attach beside the main body.

### How snapping works

Each part prefab contains predefined attachment points. For example, a fuel tank might have top, bottom, left, and right connectors.

When the player selects a part, compatible attachment points are highlighted. Clicking one attaches the part automatically with the correct position and rotation.

This eliminates the need for freeform dragging, complex placement physics, or manual alignment.

Initial restrictions:

- Maximum 10–12 parts per rocket.
- No arbitrary scaling or rotation.
- No structural damage or parts breaking off.
- All components remain connected during flight.
- No staging or detachable boosters.

These restrictions keep the construction system achievable while still allowing creative designs.

## 3. Flight simulation

The rocket flies automatically after launch. The player's engineering decisions determine its performance.

Use a simple one-dimensional vertical physics simulation:

\\[ a=\frac{T}{m}-g \\]

Where \\(T\\) is total thrust, \\(m\\) is the rocket's current mass, and \\(g\\) is gravitational acceleration.

Fuel decreases during engine operation, reducing the rocket's mass. Once fuel is exhausted, the rocket continues upward until gravity stops its ascent.

For the MVP, ignore aerodynamic drag, wind, orbital mechanics, and rotation.

Example flight results

# 2,450 m

Max altitude

# 38 s

Flight duration

# +245

Credits earned

Illustrative values, not calculated from a specific rocket.

To make launches visually satisfying, use a tracking camera, engine particles, an altitude counter, and a simple sky background. The flight can be accelerated or skipped after the player has watched it once.

## 4. Development plan

Sprint 1 — Game Framework

MVP first, then depth and polish (no fixed deadline)

- Days 1–3: Unity project, camera, construction scene, basic part prefabs.
- Days 4–6: Snap-point system, part selection, attachment and removal.
- Days 7–9: Flight simulation, altitude tracking, results.
- Days 10–12: Credits, purchasing, and UI.
- Days 13–14: Sound, testing, README, and demo.
- After the MVP: more parts and unlocks, flight feedback, balancing, polish.

Acceptance test: Assemble a rocket with at least three components, launch it, observe its altitude, and use the reward to buy another part.

Sprint 2 — Networking

Cancelled. The game is single-player.

Sprint 3 — Cloud Profiles and Saves

After Sprint 1 · Persistent profiles

- Step 1: Backend project, cloud MongoDB, health check endpoint, CORS.
- Step 2: Create and load a profile by name.
- Step 3: Save credits, unlocked parts and best altitude after flights and purchases.
- Step 4: Save, load, rename and delete rocket designs.
- Step 5: Flight history, validation, error handling (offline or server down), README.

Acceptance test: Play and unlock a part, close the browser, return with the same name, and find the credits, unlocked parts, best altitude and saved designs. Load a design, modify it, save it, and delete it.

## 5. Architecture

Unity WebGL Client

Backend API (HTTPS, JSON)

Validates requests · Owns the database credentials

Cloud MongoDB

Profiles · Rocket designs · Flight history

A player profile looks like this:

```
{
  "name": "Santi",
  "credits": 340,
  "unlockedParts": ["Capsule", "FuelTank", "Engine"],
  "bestAltitude": 2450,
  "flights": 12
}
```

Each rocket design is a small JSON structure containing part types, IDs, and attachment relationships. It is the same `RocketDesign` model used to build and simulate rockets, so saving it to the cloud needs no extra conversion.

Profiles are found by player name, with no password. This is enough for the project, but anyone can use any name.

## 6. Scope boundaries

| Include                         | Leave for later           |
| ------------------------------- | ------------------------- |
| Modular snap construction       | Freeform construction     |
| Small part catalog that grows   | Large part catalog        |
| Credits and unlockable parts    | Complex economy           |
| Automatic vertical flight       | Manual piloting           |
| Single player                   | Multiplayer               |
| Cloud profiles and saved rockets| Public rocket marketplace |
| Simple flight results           | Realistic orbital physics |
| Name-based profiles             | Accounts and passwords    |

## 7. A small interactive rocket-design experiment

Here is a simplified model to illustrate the design tradeoffs. The numbers are placeholders for balancing, not real rocket specifications.

Capsule

## 1

100 credits each

Fuel tank

## 1

80 credits each

Engine

## 1

120 credits each

Booster

## 0

160 credits each

Est. altitude

# 0 m

Total cost

# 300

Total mass

# 32

&#x20;Reset configurationThis prototype ignores drag, structural connections, and staging. Its purpose is to demonstrate how mass, fuel, and thrust create interesting decisions.

## My main recommendation

Define a single reusable `RocketDesign` data model. It should describe the parts, their unique IDs, and how they connect.

That will be the foundation for both sprints:

- Sprint 1 uses it to build and simulate rockets.
- Sprint 3 saves and loads it from the cloud.

This is the architectural decision most likely to save you time later.
