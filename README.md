# Shards of Motion

A 2D pixel-art co-op action-platform adventure where two players learn real physics (force, impulse, momentum and friction) by solving puzzles together.

Made in **Unity 6 (URP, 2D Physics)** for **Windows PC**. It's a local co-op game for **exactly 2 players** on one shared screen.

## The game

The village of Alderbrook has lost its light. Two players travel into **The Shattered Grounds** and work through eight rooms of linked physics machines. Each room teaches one idea. At the end they restart the Momentum Engine and free the Great Shard.

**Game flow:** Title → Alderbrook village → Region 1 (8 rooms, the last one is the finale) → back to the village.

| Room | Name | Physics idea |
|---|---|---|
| 1 | The First Exchange | Force and Newton's laws |
| 2 | Weight of Things | Mass and weight |
| 3 | Together We Move | Forces adding together |
| 4 | Broken Crossing | Momentum |
| 5 | Long Push | Friction |
| 6 | Weight of Cooperation | Balance and counterweights |
| 7 | Unstoppable Object | Momentum and braking |
| 8 | Heart of the Forest (finale, 3 parts) | All of the above |

Regions 2 (The Gravity Divide) and 3 (The Mechanical Depths) are planned but not built yet. They show as locked gates in the village.

### Two players, two abilities

- **Player 1: Kinetic Burst.** Pushes objects away with a single impulse.
- **Player 2: Wind Tether.** Pulls one object toward the player with a steady force.

Neither ability works on players, so every puzzle needs both of you. Both players can run, jump, float (lower gravity for a short time) and dash.

World gravity is **29.43 m/s²** (3× Earth), and every on-screen number in the game uses that value.

## Controls

| Action | Player 1 (left of keyboard) | Player 2 (right of keyboard) |
|---|---|---|
| Move | A / D | ← / → |
| Jump (hold in the air to float) | W | ↑ |
| Dash | Left Shift | Right Shift |
| Ability | F | Right Ctrl or / |
| Interact | E | Enter |

Gamepads are also supported. The game assigns controls when a room starts:
- **No gamepads:** both players share the keyboard as shown above.
- **1 gamepad:** Player 1 uses the keyboard and Player 2 uses the gamepad.
- **2 gamepads:** each player uses a gamepad.

## Getting the project running

1. Install **Unity 6000.3.22f1** through Unity Hub. Use this exact version so scenes and settings don't change between teammates.
2. Clone the repo:
   ```
   git clone https://github.com/peyyyaa/VECTOR-Shards-of-Motion-2026-11-12_17-07-47.git
   ```
   You can also use GitHub Desktop → *Clone repository*.
3. In Unity Hub, choose **Add → Add project from disk** and pick the cloned folder.
4. Open the project. The first import takes several minutes because Unity rebuilds the `Library` folder, which isn't stored in git.
5. Open `Assets/_Project/Scenes/Title.unity` and press **Play**.

To jump straight into the rooms, open `Assets/_Project/Scenes/Regions/Region1_ShatteredGrounds.unity`.

## Project layout

```
Assets/
  _Project/            all of our own work
    Scenes/            Title, Village, Regions/Region1_ShatteredGrounds, Prototype/ (test scenes)
    Scripts/           Player, Abilities, Puzzle, Interactables, Rooms, Village, UI, Core, FX, Editor
    Prefabs/           players, room prefabs, puzzle pieces
    Art/ Audio/ Data/  sprites, sounds, journal entries and other data
  ThirdParty/          bought or free art packs (see Credits)
  Cainos/              Cainos art packs (Village Props, Interactive Pixel Water)
Docs/                  design docs, status and plans
CLAUDE.md              project rules (design, physics, co-op, input)
```

Useful docs:
- [Docs/Status_Dec1.md](Docs/Status_Dec1.md): current scope, what's done and what's next
- [Docs/Room_Building_Guide.md](Docs/Room_Building_Guide.md): how rooms are built
- [Docs/Region1_Art_Direction.md](Docs/Region1_Art_Direction.md) and [Docs/Village_Art_Plan.md](Docs/Village_Art_Plan.md): art direction

### Editor tools

Most rooms and scenes are built by editor scripts, found under **Tools → Shards of Motion** in the Unity menu bar. For example, to build a room:
1. **Room N → 1. Build Graybox**
2. **Room N → 2. Apply Visual Pass**
3. **Journal → Draw Room Sketches**

Run these with `Region1_ShatteredGrounds` open.

## Working together

- Pull before you start (`git pull`) and push when you finish, so nobody works on an old copy.
- **Avoid editing the same scene at the same time.** Unity scene files merge badly. Agree in chat who has `Region1_ShatteredGrounds.unity` or `Village.unity` open.
- Commit both the `.meta` files and the assets they belong to. A missing `.meta` file breaks references for everyone else.
- Gameplay code reads input only through `PlayerInputReader` (one per player). Don't read the input asset directly, or both players will react to the same keys.
- Don't change gravity, masses or ability strengths to make a puzzle easier. The puzzle numbers are part of the lessons.

## Credits

- **Legacy Fantasy – High Forest** by Anokolisa (itch.io): backgrounds, trees, foliage, props
- **Humble Gift – Paper UI System** by Humble Pixel (@Humblepixel): UI panels
- **Pixel Art Platformer – Village Props** and **Interactive Pixel Water** by Cainos: tiles, props and water
- Some building and title art was generated with PixelLab and adapted by the team.

See [Assets/ThirdParty/CREDITS.md](Assets/ThirdParty/CREDITS.md) for licence notes. **The Humble Paper UI licence doesn't allow the files to be redistributed, so keep this repository private.**
