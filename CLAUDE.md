# GAME DEVELOPMENT PROJECT — PROJECT INSTRUCTIONS

## Project Overview

We are developing an original 2D pixel-art physics action-platform adventure using Unity 6, C#, and URP.

Target platform: Windows PC.

Team size: 2–4 people.

Development schedule: Approximately 8 weeks.

## Game Direction

Genre: 2D action-platform adventure.

Camera: Side-view, room-based camera (one shared screen for both players).

Players: Local co-op only — always exactly 2 players.

Visual style: Detailed fantasy pixel art.

Animation: Frame-by-frame sprite animation.

Gameplay balance:
- Platforming
- Combat
- Physics-based puzzles
- Exploration
- Educational physics concepts

## Local Co-op

- Co-op only: exactly 2 players on one shared screen. No single-player mode.
- Each player has different abilities. Core movement (below) is shared by both.
- Ability-to-player mapping: TO BE DECIDED by the team (see region abilities below).
- Puzzles must be designed for two players; room exits require both players.
- Controls: shared keyboard (P1 left half, P2 right half), keyboard + gamepad, or two gamepads.
  `LocalCoopSpawner` assigns devices when a room starts.
- Input rule: gameplay scripts read input only through `PlayerInputReader`
  (one per player, backed by Unity's Player Input component). Never read the shared
  input asset directly, or both players will respond to the same keys.
- Players do NOT collide with each other (Player × Player disabled in the Layer Collision
  Matrix): they pass through each other and can push the same object together (forces add).
  No stacking on each other; reaching high places uses partner-launching instead.
- Pressure plates count the whole stack's weight (e.g., a player standing on a block on the plate).

## Core Movement

Each player should eventually support:
- Horizontal movement
- Jumping
- Temporary floating (reduced gravity)
- Air control
- Horizontal dash

Movement should feel responsive and controllable.

## Game Regions

### 1. The Shattered Grounds
Physics topics:
- Newton's laws
- Force
- Acceleration
- Impulse
- Momentum

Ability: Kinetic Burst.

### 2. The Gravity Divide
Physics topics:
- Projectile motion
- Velocity
- Gravity

Ability: Wind Tether.

### 3. The Mechanical Depths
Physics topics:
- Work
- Kinetic energy
- Potential energy
- Conservation of energy

Ability: Kinetic Bloom.

The finale combines the abilities and concepts.

## Technical Requirements

- Unity 6
- Universal Render Pipeline
- C#
- Unity 2D Physics
- Rigidbody2D and Collider2D for physics interactions
- Orthographic camera
- Pixel-art-compatible rendering
- Prefer simple, maintainable implementations.

## Development Principles

1. Do not implement large unrelated systems.
2. Inspect existing files before modifying them.
3. Explain the purpose of scripts and Unity components.
4. Do not invent existing GameObjects, assets, or project settings.
5. Do not change unrelated systems.
6. Avoid unnecessary packages and dependencies.
7. Keep the project appropriate for a small team and an eight-week schedule.
8. Separate gameplay logic into understandable scripts.
9. Use serialized Inspector references where appropriate.
10. Explain all required Unity Inspector configuration.

## Physics Requirements

- Check equations, units, and assumptions.
- Distinguish ideal theoretical physics from Unity's simulated behavior.
- Use ForceMode2D.Impulse for instantaneous impulses.
- Use ForceMode2D.Force for continuous forces.
- Do not confuse mass, force, impulse, and velocity.
- Explain the physics behind each educational mechanic.

## Current First Milestone

Build one graybox prototype room containing:

1. Player movement
2. Jumping
3. Temporary floating
4. Dash
5. Rigidbody2D physics block
6. Kinetic Burst ability
7. Pressure plate
8. Exit door

Do not create the entire game at once.

## Collaboration Rules

Work in small, reviewable milestones.

Before implementing a feature:
1. Inspect relevant files.
2. Explain the proposed approach.
3. Identify required Unity components.
4. Implement only the requested feature.
5. Explain how to configure it.
6. Provide a practical Unity testing checklist.

ChatGPT serves as the independent design mentor, physics reviewer, and code reviewer. Claude Code handles targeted implementation.