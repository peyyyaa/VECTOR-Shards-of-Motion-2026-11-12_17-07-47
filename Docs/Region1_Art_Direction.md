# Region 1: The Shattered Grounds — Art Direction

Status: **approved direction** (visual reference board + decisions below). Room 1 is the visual benchmark;
the style is applied to the other seven rooms only after Room 1 is signed off.

---

## 1. Visual identity

- 2D pixel art, polished fantasy **forest ruins**: an abandoned forest civilization reclaimed by nature.
- Rich greenery, moss-covered stone, wooden mechanisms, waterfalls, ancient magical elements.
- **Cyan/teal magic** contrasting with natural forest colors; **gold** marks interactive objects.
- Readable silhouettes. Gameplay paths and physics objects must stay easy to read: decoration never
  clutters the play space.

**Palette swatches** (from the reference board): Forest/Foliage (deep green) · Grass/Moss (bright green) ·
Stone/Ruins (cool grey-blue) · Wood/Mechanisms (warm brown) · Water/Magic (cyan) · Accent, UI/Interactive (gold).

The reference board is a **mood board**: use it for palette, composition and mood. Final assets are made or
sourced to fit our grid and measurements, not traced from it.

---

## 2. Technical rules (every asset)

| Rule | Value |
|---|---|
| Pixels per unit | **32** (1 tile = 32 px = 1 unit = 1 m) |
| Tile size | **32 × 32 px** |
| Filter mode | **Point (no filter)** |
| Compression | **None** |
| Scaling | Whole sprites only, **no non-integer scaling** |
| Rotation | Only physics objects that are meant to rotate (blocks, rolling boulders, seesaw planks) |
| Screen | 640 × 360 reference (Pixel Perfect Camera), 20 × 11.25 units per room |

Gameplay geometry, colliders and physics values (masses, friction, forces) are **never changed to fit visuals**.
Art is fitted to the graybox, not the other way round.

---

## 3. Decisions (approved)

### 3.1 Ruins tileset, foliage and backgrounds: hybrid
1. **Find compatible free** pixel-art assets first: mossy ruins / stone terrain, foliage, forest backgrounds.
   They must match the technical rules (section 2) and the palette. **Don't add visually incompatible packs**
   just to fill a category.
2. **Village Props** (already imported) for wooden mechanisms, selected props, rocks, logs, lanterns, signs.
3. **Make or customize** the missing details: moss overlays, hanging vines, puzzle markings,
   region-specific decorations.
4. Don't build a whole tileset from scratch unless nothing compatible exists.

### 3.2 Rotation by object type
| Object | Rotation |
|---|---|
| Regular puzzle blocks | **Free** — blocks tip and tumble, so their weight visibly affects how they behave (decision revised after play-testing: frozen rotation felt less physical) |
| Rolling boulders | Free (rolling is the point) |
| Seesaw plank | Free around its hinge (±15° limits) |

Art consequence: block sprites will be seen at any angle, so design them to read well rotated
(simple, symmetric silhouettes; mass glyphs that stay readable at an angle). Slight pixel shimmer while
tumbling is accepted.

### 3.3 Lit materials
- Convert **only the environment sprites we actually use** to Sprite-Lit, using **project-owned material
  copies** in `Assets/_Project/Art/Materials/`. Imported package materials are left untouched.
- Test them in a separate scene with the active URP 2D Renderer and confirm the original pixel colors are
  unchanged under neutral (white, intensity 1) global light.

### 3.4 Rooms as prefabs
- Each room is a prefab in `Prefabs/Rooms/Region1/` containing its own terrain, puzzle objects, hazards,
  entry/exit points and local decoration.
- Shared systems stay at scene level: players (spawner), camera, RoomManager, UI canvas, region background.
- Room transitions, object persistence and room reset must keep working (see 6.3).

---

## 4. Asset mapping (existing vs. missing)

| Reference element | Existing asset | Fit |
|---|---|---|
| Mossy stone terrain, walls | Village `TX Tileset Ground` (brown dirt cobble) | Poor — placeholder only |
| Broken ruins (arches, pillars) | Village brick-rubble pile | Partial (one piece) |
| Wooden bridges, scaffolds, stairs | Village platforms, scaffolds, stairs | **Good** |
| Movable blocks | Village crates | Partial (placeholder; stone blocks with mass glyphs preferred) |
| Seesaw | Village `Seesaw 01` (our Seesaw prefab) | **Good** |
| Moving platform / lift | Village planks, `Elevator` art (our Mover) | **Good** |
| Reset shrine | Village `Stone of Recall` (cyan glyph) | **Very good** |
| Journal stone | Village signs | Partial (stone tablet with book icon preferred) |
| Lanterns, torches | Village lantern, torch, fire bowl (animated flames) | **Good** |
| Banners | Village red banner | Partial (needs green/gold) |
| Trees, bushes, grass, flowers | Village trees, bushes, grass | Partial (shapes OK, palette too muted/olive) |
| Rocks, logs, stumps | Village rocks, logs, stump | **Good** |
| Water pools / channels / hazards | Interactive Pixel Water (Clear, Acid, Lava, Poisonous, Muddy) | **Good** |
| Waterfalls | — | Missing |
| Vines, roots, moss overlays | — | Missing |
| Pressure plate, lever, gate, water gate, current FX | — (graybox) | Missing |
| Region landmark (shard) | — | Missing |
| Characters | — | Missing |
| Backgrounds (parallax) | — | Missing |
| UI frames, key icons | Built-in rounded panel; Pixie font (headings) | Partial |

**Not used in Region 1:** wheat, scarecrows, market stall, graveyard, weapons, farm tools (village/farm theme);
Jump and Bounding Platforms (saved for Region 2).

**Import notes:** Village textures currently use compression → set to **None** (import setting only).
Village materials are unlit → use project-owned Sprite-Lit copies (3.3).

---

## 5. Sorting layers and Tilemaps

### Sorting layers (back to front)
| # | Layer | Contents |
|---|---|---|
| 1 | **Background** (exists) | Sky, parallax far/mid layers (≤ 60% brightness, desaturated) |
| 2 | **BackDecor** (new) | Ruin back walls, back vines, distant waterfalls — no colliders |
| 3 | **Midground** (exists) = terrain | Collidable Tilemaps, ledges |
| 4 | **Props** (new) | Decorative props on terrain — never interactive-looking |
| 5 | **Interactables** (exists) | Blocks, plates, levers, gates, movers, shrines, Journal stones |
| 6 | **Player** (exists) | P1, P2 |
| 7 | **Water** (new) | Pixel Water surfaces (objects inside look submerged) |
| 8 | **FX** (new) | Burst, Tether, splashes, respawn |
| 9 | **Foreground** (exists) | Overhanging vines, foreground leaves — sparse, never covering puzzles |

UI is drawn by the screen-space UI canvas (separate from sorting layers).

### Tilemaps (inside each room prefab)
```
RoomXX
  Layout
    Grid (cell 1 x 1)
      TM_BackWall      BackDecor    no collider
      TM_Terrain       Midground    Ground layer; TilemapCollider2D (Used By Composite)
                                    + CompositeCollider2D + Static Rigidbody2D
      TM_Terrain_Wet   Midground    same, Wet_Stone material   (friction rooms only)
      TM_Terrain_Moss  Midground    same, Moss material        (friction rooms only)
      TM_Decor         Props        no collider
      TM_Foreground    Foreground   no collider
  Puzzle / Objects / Hazards / Info
_Background (scene level): parallax layers following the camera
```
The composite collider gives each room one smooth terrain collider (no floor seams snagging blocks).

---

## 6. Puzzle-object visual language

Every interactive object has three readable states, **without labels**:

| State | Look |
|---|---|
| Inactive | Dull stone / plain wood |
| Active | **Cyan glow** or lit, gold highlights |
| Transitioning | Animated (lever swinging, gate moving, plate pressing) |

- Interactive objects alone use **gold accents** and **cyan glow**; decorative props never do.
- Block masses must be distinguishable by **size or glyph markings**, not color alone (plus the mass label).
- Current zones show direction (flowing streaks/arrows); water gates show open/closed clearly.

### 6.3 Room-prefab compatibility checks
Room reset finds a room's objects by **parent Room** (prefab children) or **home point**; exits use their
parent Room, else position. Doorways stay at scene level (`Connections`), never inside a room prefab
(they sit on the boundary between two rooms).

---

## 7. Characters

| | P1 — Kinetic Burst | P2 — Wind Tether |
|---|---|---|
| Palette | Warm: red, orange, brown | Cool: white, cyan, blue |
| Silhouette | Spiky hair, scarf, sturdy stance | Long/light hair, cloak or robe, lighter stance |
| Ability FX | Orange impulse arc (60° cone, 2.5 m = 80 px) | Cyan wind beam + swirls (4 m reach) |

- About **48 px tall** (1.5 units), body ~26 px wide (collider 0.8 × 1.5; hair/cloth may extend).
  Pivot bottom-center, 32 PPU, Point, no compression. Side view; left = flipped.
- Distinct **silhouettes**, not only colors (players overlap).

| Animation | Frames | fps |
|---|---|---|
| Idle | 4–6 | 8 |
| Run | 6–8 | 12 (stride matches 6 m/s) |
| Jump rise / apex / fall | 2 / 1–2 / 2 | 12 |
| Land | 2 | 16 |
| Float (hair/cloth flutter) | 4 loop | 10 |
| Dash + afterimage | 3–4 | 24 (dash lasts 0.16 s) |
| Push | 6 | 10 |
| P1 Burst (release frame = impulse) | 4–6 | 16–20 |
| P2 Tether start / hold / release | 3 / 4 loop / 2 | 12 |
| Interact (lever) | 3–4 | 12 |
| Hazard death / respawn | 6 / 6 | 12 |

A small `PlayerAnimator` script will read existing states (grounded, velocity, IsFloating, IsDashing, tether
target) — no movement or ability changes.

---

## 8. Room 1 benchmark (The First Exchange)

The Room 1 benchmark asset list (reused vs. missing) is tracked separately once approved. Summary of the rules every room copies:

1. **Values:** backgrounds ≤ 60% brightness and desaturated; terrain mid-value; interactables brightest.
2. **Accents:** gold and cyan only on interactive objects.
3. **States** readable without labels (section 6).
4. **Clear paths:** no decoration on jump arcs, block paths or plate tops; foreground only at screen edges.
5. **No scaled/rotated pixel art** except physics objects that rotate (blocks, boulders, seesaw planks).
6. **Lighting:** global light + 2–3 small 2D lights (lanterns, exit arch, shrine), project-owned lit materials.

**Acceptance:** P1/P2 told apart in < 1 s even overlapping · every interactive object recognizable with labels
off · readable at 1080p and 720p · no compression/filter artifacts, all 32 PPU · plays exactly like the
graybox (same colliders and measurements) · stable frame rate with water and lights on.

---

## 9. Room-by-room direction

Progression: from the **dim, sparse outer ruins** to the **lush, glowing Heart of the Forest** — magic and
light increase as the players go deeper.

| Room | Mood / setting | Landmark | Palette emphasis | Water |
|---|---|---|---|---|
| 1 First Exchange | Edge of the ruins, morning, sparse vines | Glowing exit arch | Stone + green | Background waterfall only |
| 2 Weight of Things | Ancient testing hall, three pedestals, carved tunnel | Carved scales relief | Cool stone, gold markings | Current lane |
| 3 Together We Move | Broken causeway over an acid pit | The moving bridge | Green acid glow | Acid pool |
| 4 Broken Crossing | Collapsed aqueduct | Sluice water gate with wheel | Wood + teal | Channel, water gate, waterfall |
| 5 Long Push | Flooded corridor | Paired-lever double gates | Blue wet stone vs green moss | Current, wet floor |
| 6 Weight of Cooperation | Inner sanctum, balance carvings | The lift | Warm lanterns, gold | — |
| 7 Unstoppable Object | Steep ravine with roots | Seesaw on a stone pedestal, boulder slope | Deep greens, darker | Pit below (hazard) |
| 8 Heart of the Forest | Great tree grown through a temple | The shard (cyan crystal) | Full saturation, strongest cyan | Pools, falls |

Consistency across all rooms: same tileset, same interactive-object sprites, same sorting layers and lighting
rig, one landmark per room.
