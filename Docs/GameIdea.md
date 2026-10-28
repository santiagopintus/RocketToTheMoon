I think collaborative rocket building is a strong fit for all three modules, especially because the multiplayer and database features become natural parts of the game rather than unrelated additions.

The biggest challenge will be controlling the scope. I'd design it as a 2-player cooperative rocket-building sandbox, with a simple 3D construction system and lightweight flight simulation.

## Game concept

[SIMPLE | Space Cartoon Assets | Synty™ Official Store  – Synty Store](https://images.openai.com/static-rsc-4/112nRs7_h83KxbnZQ0zeeBB7BDotDoNVB_JcidndIBqq8DW0QpRejcya2pSgyx4MY-mG2OPRAG67Ab9de5mvFBKs0E-YijdjCNmQqVVyYwBUGLUVxrhV09cZBiPRcWWKi-7EojOdGiYHxTPVzI-gstTKcPBK4KquBY0mjq75Bdk?purpose=inline)

[syntystore.com](https://syntystore.com/products/simple-space-cartoon-assets?utm_source=chatgpt.com)

Working concept: Rocket Workshop

Two players collaborate to design a rocket using purchased modular parts. They experiment with different configurations, launch their creation, and try to reach the highest possible altitude.

There are no characters to control. Players interact directly with the rocket, its parts, and the construction interface.

Core gameplay loop:

Buy parts

Build rocket

Launch

Measure altitude and earn credits

Improve the rocket and try again

## Development across three sprints

Sprint 1 — Game Framework (Weeks 1–2)

Build the entire single-player gameplay loop locally.

- 3D construction scene and orbiting camera.
- Three parts: command capsule, fuel tank, engine.
- Click to select parts and attach them at predefined snap points.
- Launch button and basic vertical flight physics.
- Maximum altitude, credits, and restart.
- Sound effects.

Deliverable: A player can assemble and launch a rocket without networking.

Sprint 2 — Networking (Weeks 3–4)

Add two-player collaboration through a client-server architecture.

- One player hosts; another joins using an IP address.
- Both players see the same rocket.
- Adding, removing, or changing parts synchronizes across clients.
- Either player can initiate a launch.
- The server calculates or authoritatively distributes the launch results.

Deliverable: Two devices can collaboratively design and launch the same rocket over LAN.

Online play can be a stretch goal; LAN is enough for the assignment.

Sprint 3 — Cloud Databases (Weeks 5–6)

Add persistent rocket designs and flight history.

- Save a rocket design to the cloud.
- Load saved designs.
- Rename or modify existing designs.
- Delete designs.
- Store launch results and personal best altitude.
- Optionally store designs and launch records in related collections.

Deliverable: Players can preserve their creations and continue experimenting across sessions.

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

Player 1

Unity Client

Player 2

Unity Client

C# Game Server

TCP · Room state · Build validation · Launch results

Cloud Database

Saved rockets · Flight records

For Sprint 2, I'd use TCP with JSON messages rather than building full real-time multiplayer physics. Since players are only editing a shared rocket, you can synchronize discrete actions such as `AddPart`, `RemovePart`, `PurchasePart`, and `Launch`.

The server validates each action and broadcasts the updated rocket state to both clients. This avoids synchronizing physics objects every frame.

For Sprint 3, Firebase Firestore is a reasonable option. Keep database credentials on the server, not inside the Unity client.


## Modular construction — Parts snap to predefined attachment points, including side-mounted tanks and engines. More creative freedom.

# Rocket Workshop — 6-Week Game Design Plan

A cooperative 3D rocket-building game where two players design, purchase, assemble, and launch modular rockets to reach the highest possible altitude.

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

Weeks 1–2 · Single-player MVP

- Days 1–3: Unity project, camera, construction scene, basic part prefabs.
- Days 4–6: Snap-point system, part selection, attachment and removal.
- Days 7–9: Flight simulation, altitude tracking, results.
- Days 10–12: Credits, purchasing, and UI.
- Days 13–14: Sound, testing, README, and demo.

Acceptance test: Assemble a rocket with at least three components, launch it, observe its altitude, and use the reward to buy another part.

Sprint 2 — Networking

Weeks 3–4 · Two-player collaboration

- Days 1–3: TCP server and JSON message protocol.
- Days 4–6: Host/join UI and synchronized rocket construction.
- Days 7–9: Shared credits, purchases, and part removal.
- Days 10–12: Synchronized launch and results.
- Days 13–14: Two-device testing, reconnection handling, README.

Acceptance test: Two clients connect to the same server, add parts to a shared rocket, and receive identical launch results.

Sprint 3 — Cloud Databases

Weeks 5–6 · Persistent designs

- Days 1–3: Configure Firestore and server integration.
- Days 4–6: Save and load rocket designs.
- Days 7–9: Update and delete saved designs.
- Days 10–12: Flight history and personal best records.
- Days 13–14: Validation, error handling, README, and final demo.

Acceptance test: Save a rocket, close the game, reconnect, load the rocket, modify it, save the changes, and delete the design.

## 5. Architecture

Unity Client A

Unity Client B

C# TCP Server

Validates construction · Manages shared state · Calculates flight

Firebase Firestore

Rocket designs · Flight history

The clients send commands, not entire rocket models. For example:

```
{
  "type": "AddPart",
  "partType": "FuelTank",
  "parentPartId": "part_001",
  "socketId": "side_left"
}
```

The server validates the command, updates the shared rocket state, and sends the result to both players.

Each rocket design can be represented as a small JSON structure containing part types, IDs, and attachment relationships. This same structure can be reused for multiplayer synchronization and cloud saves.

## 6. Scope boundaries

| Include in the six-week project | Leave for later           |
| ------------------------------- | ------------------------- |
| Modular snap construction       | Freeform construction     |
| 4 basic part types              | Large part catalog        |
| Shared currency                 | Complex economy           |
| Automatic vertical flight       | Manual piloting           |
| Two-player collaboration        | More than two players     |
| LAN via IP                      | Public matchmaking        |
| Cloud save/load                 | Public rocket marketplace |
| Simple flight results           | Realistic orbital physics |

One technical detail: Supporting online play over the public internet is a separate challenge from LAN. A TCP server running on a publicly reachable host could support both, but NAT traversal, hosting, and security add work. I'd guarantee LAN for Sprint 2 and treat internet play as an optional improvement.

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

Before writing any networking or database code, define a single reusable `RocketDesign` data model. It should describe the parts, their unique IDs, and how they connect.

That will be the foundation for all three sprints:

- Sprint 1 uses it to build and simulate rockets.
- Sprint 2 synchronizes it between players.
- Sprint 3 saves and loads it from the cloud.

This is the architectural decision most likely to save you time later.