# Room Building Guide — Shards of Motion

How to build rooms that fit the camera, connect correctly, and suit 2-player co-op.
All distances are in Unity units (1 unit = 1 tile = 32 pixels = 1 meter).

---

## 1. Room size

| Measurement | Value |
|---|---|
| One room = one screen | **20 × 11.25 units** (640 × 360 px at 32 PPU; scales ×3 to 1920 × 1080) |
| Room centers, side by side | 20 units apart (x) |
| Room centers, stacked | 11.25 units apart (y) |
| Floor top (Room_Standard) | room center − 4.5 |
| Inside width between the walls | 18 units |
| Height from floor top to screen top | ≈ 10.1 units |

The camera never scrolls inside a room, so everything in the room is always visible.

---

## 2. Player and ability measurements

Use these to decide what is reachable. The numbers come from the scripts (g = 29.43 m/s²).
Air moves are calculated for ideal conditions: always confirm them in play-testing.

| Move | Measurement | Design meaning |
|---|---|---|
| Player size | 0.8 wide × 1.5 tall | Gaps need to be at least 1 tall and 1 wide to fit through |
| Walk speed | 6 m/s | |
| Jump height | 2.5 m (feet) | A ledge **≤ 2 m** is an easy climb. **2.5 m** is the absolute limit. **≥ 3 m** can't be reached without a step. |
| Jump across a flat gap | ≈ 4.9 m | Jump only |
| Jump + float | ≈ 9.5 m | Float slows the fall only (it never adds height). It lasts 1 s, and landing refills it. |
| Jump + float + air dash | ≈ 11.5 m | Dash 3 m horizontal, once in the air, 0.5 s cooldown |
| Push (walking into a block) | 12 N per player | **One player can't move a 2 kg block. Two players together can.** |
| Kinetic Burst (P1) | 15 N·s, 2.5 m range, 60° front cone | Sends a 2 kg block ≈ **2.9 m** along the floor |
| Wind Tether (P2) | 24 N pull, 4 m range, 120° front cone | Needs line of sight. Moves a 2 kg block slowly (≈ 2.2 m/s²). Walls block it. Players don't. |
| Block as a step | Standing on a 1 m block + jump | Reaches a ledge up to **3 m** high (0.5 m to spare at 3 m) |
| Pressure plate | Needs 45 N, and the body has to be settled (< 0.5 m/s) | 1 player (29 N) is **not** enough. 2 players, or a 2 kg block (59 N), are enough. |

---

## 3. Building a room with the prefabs

Prefabs are in `Assets/_Project/Prefabs/Rooms/`.

### Room_Standard
A ready-made empty room containing:
- The **Room** component.
- A floor.
- Left and right walls.
- Two default entry points (used to start the game in this room, and for respawns).

1. Drag **Room_Standard** into the scene and **rename it** (e.g., `SG_Room03`).
2. Set its position to the room's center. The next room to the right is **+20 x**.
3. Build the room's contents (ledges, plates, blocks) as children of the room, using the
   same graybox squares (layer **Ground** for terrain).

### Doorway_Side
Connects two side-by-side rooms. It contains:
- The wall above the opening.
- The player-only barrier.
- One exit in each direction.
- The arrival points.

**It needs no setup:** each exit works out which room it belongs to and where it leads from its own position.

1. In both rooms, **delete the wall on the shared side**: the left room's `Wall_Right` and the right room's `Wall_Left`.
2. Drag **Doorway_Side** into the scene. Position it at:
   **x = the boundary between the rooms**, **y = the rooms' floor top** (room center y − 4.5).
   - Example: rooms at x = 0 and x = 20 → Doorway at **(10, −4.5)**.
3. Check the green exit boxes: there should be one on each side of the boundary, each inside its own room's yellow outline.

The opening is **4 units tall** from the floor. Players stop at the barrier, but **blocks pass through**
(layer PlayerBarrier collides with Player only). That is what lets puzzles span more than one room.

### Starting room
`RoomManager` → **Starting Room** = the first room. Make the spawner's `P1_Spawn` / `P2_Spawn`
match that room's entry points, so respawns return players to where they started.

---

## 4. Co-op design rules

1. **Room exits need both players.** The first to arrive waits at the edge.
2. **Give both players a job at the same time.** Avoid one player just watching.
3. **Use both abilities.** Burst (P1) pushes away; Tether (P2) pulls toward. Rooms where you
   need *both directions* are the strongest co-op puzzles.
4. **One new idea per room.** Teach it alone first, then combine it with earlier ideas in later rooms.
5. **Check for "single-player solutions".** In a flat room, two bursts or a two-player push
   can replace the tether. Use walls, gaps or heights to force the intended solution if it matters.
6. **Keep platforming forgiving.** Puzzle steps should stay well inside the measurements in section 2.
   Don't make co-op puzzles depend on the jump limit.
7. **Show the physics.** A sign or readout (mass, weight, force) in the room teaches the concept
   without a separate tutorial.

---

## 5. Making the world feel big

- **Puzzles across rooms:** carry or burst a block from one room into another. A plate in one room can open a gate in another.
- **Shortcuts:** a gate that only opens from the far side, looping back to an earlier room.
- **Views of later areas:** show a later area or region in the background.
- **Room state is kept automatically:** everything stays where the players left it, because all rooms
  of a region are in one scene.

---

## 6. Before play-testing a room: checklist

- [ ] Yellow room outlines touch edge to edge and don't overlap.
- [ ] Every doorway has a green exit box on **each** side, each inside its own room.
- [ ] Arrival points aren't inside an exit box, a wall, or where a block usually rests.
- [ ] Every height and gap the solution needs fits section 2, and the ones meant to be impossible really are.
- [ ] The puzzle needs both players (no single-player solution, unless that's intended).
- [ ] Blocks that should cross a doorway really do slide across (see the floor seam note below).
- [ ] Run **Tools → Shards of Motion → Organize Hierarchy** (section 7).
- [ ] **Ctrl+S** (scene) and, after any Project Settings change, **File → Save Project**.

---

## 7. Hierarchy organization

Every scene uses the same Hierarchy layout. Run **Tools → Shards of Motion → Organize Hierarchy** after
building or changing a room. It sorts everything automatically, keeps every object's position and size, and
can be undone with Ctrl+Z.

```
_Managers       camera, lights, RoomManager, CoopSpawner, spawn points
Room01          the Room object (rooms listed left to right), containing:
   Layout       floors, walls, ledges, shelves (static geometry)
   Puzzle       plates, levers, relays, doors/gates, movers, currents, shrines, seesaws
   Objects      blocks / crates
   Hazards      acid / lava pools
   Info         signs, Motion Journal stones
Connections     doorways, room exits, player barriers, arrival points
_Unsorted       anything outside every room's area (check these!)
```

Rules:
- An object belongs to the room whose area contains its **centre**.
- **Doorways and room exits always go in Connections, never inside a Room.** An exit finds its room through
  its parent Room first, and a doorway sits on the boundary between two rooms. Nested under one of them,
  the exit leading the other way would think it belongs to the wrong room.
- Anything that ends up in **_Unsorted** is outside every room. Usually it's misplaced: move it into a room
  and run the tool again.

---

## 8. Known limitations

- **No vertical doorway yet.** Rooms can be stacked, but there is only a sideways doorway prefab. A doorway
  in the floor or ceiling (shafts, drops) is a planned addition.
- **Floor seams:** each Room_Standard has its own floor. Where two floors meet at a doorway, a sliding
  block can sometimes catch on the joint. If a block must slide across, test it. If it catches, replace the
  two floors with one long floor (as Prototype_Room01/Room02 do).
- **Players are moved, not walked,** through a doorway. They appear at the arrival points while the
  camera slides (0.4 s).
- **Respawn** returns a player who leaves the room to where they entered it. Blocks aren't reset.
