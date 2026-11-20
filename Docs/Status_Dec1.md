# Shards of Motion — Status & Plan to December 1, 2026

Read this first in a new session. Rules of the project are in `CLAUDE.md`; this file is the current scope,
what's done, how the tools are used, and what's next.

## Scope for December 1 (agreed Nov 20)

A complete, playable game:
**Title → Alderbrook village → Region 1 "The Shattered Grounds" (8 rooms) → finale → back to the village (ending, credits).**

- **Region 1: all 8 rooms.** Rooms 1–4 done. Rooms 5–8 to build; **Room 8 = finale** ("Heart of the Forest"):
  the machine parts complete the **Momentum Engine** and its puzzle frees the Great Shard.
- **Regions 2 (Gravity Divide) and 3 (Mechanical Depths): cut for now**, shown as locked gates in the village ("coming soon").
  Also deferred: Kinetic Bloom, launchers/cannons, circular-motion mechanics, extra village screens and shops.
- **Village (core):** 3 screens (see `Docs/Village_Art_Plan.md`): **Wren's Lab** (research table with machine parts,
  wall map), **Market Square** (Mira's cottage, Field Requests board, fruit stall, shard shrine, well) and
  **Forge & East Road** (Bram's forge = Village Fund, road gate with region signs). Wren (he), Bram and Mira,
  journal-style speech boxes, ambient life, **restoration** projects that show progress.
- **Shop: Village Fund** — spend shards (a balance; the collection record is kept for 100%) on 3–4 visible restoration
  projects. **No power upgrades**: puzzle numbers (burst 15 N·s, tether 24 N, push 12 N, g = 29.43) must stay fixed.
- **Map:** parchment map (lab wall + pause menu): rooms, shards x/3, parts, journal read, locked regions;
  travel to any room already reached.
- **Sound:** effects generated in code; music: 1–2 free tracks sourced by the team (needed ~Nov 27).

## Schedule (11 days; tight — trim from the bottom of "If we fall behind")

| Days | Work |
|---|---|
| Nov 20–22 | Village core: screens, villagers, speech boxes, layout, ambience (**done Nov 20**) |
| Nov 23 | Map (with travel) + Village Fund shop |
| Nov 24 | Room 5 |
| Nov 25 | Room 6 |
| Nov 26 | Room 7 |
| Nov 27 | Room 8 finale (Momentum Engine) |
| Nov 28 | Ending (assembly at the lab, restoration, credits), lock Regions 2–3, sound effects + music |
| Nov 29 | Full playthrough New Game → credits, balance, bugs |
| Nov 30 | Windows build, keyboard/controller test on a real build, performance |
| Dec 1 | Buffer / final fixes |

**If we fall behind, trim in this order:** ambient extras → 2nd villager → map travel (keep info) → shop projects 4→2
→ Room 7 simplified (one mechanic) → sound effects reduced to the core set.

## Done (all committed)

- Movement, float, dash, Kinetic Burst (P1), Wind Tether (P2), co-op spawning, rooms/doorways, reset shrines, hazards.
- Characters P1/P2 (48x60 pixel art from text sources, 9 animations, PlayerAnimator), ability & movement FX.
- Motion Journal (field-notebook UI, force diagrams, page turning, Wren's sketch page, Wren's tips).
- Rooms 1–4 (graybox + art): 1 First Exchange, 2 Weight of Things, 3 Together We Move (cargo ferry), 4 Broken Crossing (momentum, rubble walls).
- Game shell: save system (auto, one slot), title screen, pause menu, settings, dev tools.
- Title screen v2: PixelLab forest-ruin painting (Art/Title), village crystal floating in the arch, P1/P2 framing it.
- Village core: 3 screens with PixelLab buildings (adapted: Art/Village; sources in Art/Village/Source), chibi Wren/Bram/Mira
  (idle + action, NpcActivity), dialogue, VillageAmbience (birds, smoke, embers, sway, pollen, motes),
  RestorationProject states (well, lamps, garden, shrine; broken until funded). Dev menu: Village: Restore / Unrestore All.

## Tools (Tools > Shards of Motion) — typical run order

- **New room:** `Room N > 1. Build Graybox` → `Room N > 2. Apply Visual Pass` → `Journal > Draw Room Sketches`
  (Region1_ShatteredGrounds open, so raised doorways are read).
- **Art:** `Characters > 1. Build Character Sprites` (P1, P2, Wren...), `Effects > 1. Build Effect Sprites`,
  `UI Kit > 2. Build UI Kit`, `Create UI Prefabs` (canvas: journal, pause, settings).
- **Text:** `Journal > Update All Entries` (entries + tips; keeps drawn sketches).
- **Shell:** `Game Shell > Build Title Scene`, `Game Shell > Build Village` (run Characters > 1. Build Character Sprites
  first; Build Village before Build Title Scene, which uses the village crystal).
- **Testing:** `Dev > Play From Room > Room N`; in play: PgDn/PgUp rooms, F9 reset, F10 hold gates.

## Conventions

- Rooms are 20 x 11.25 m; room-local floor top y = -4.5; doorways at x = ±10 (adjustable opening height: `Doorway`).
- Room builders (`RoomNBuilder`) clone Room 1's graybox parts via `RoomBuildKit`; `RoomDresser` dresses by type;
  a short per-room `RoomNDressing` adds decoration. Everything they make is re-runnable.
- Physics values never change to fit art. Player body 0.8 x 1.5 m (scaled 1x1 collider), 1 kg. Crates 1/2/3 kg, mu = 1/3.
- Gameplay reads input only through `PlayerInputReader`; menus/dev keys may read devices directly (documented).
- Commit after the user tests and confirms; push only when asked (pushing is blocked by the permission settings).

## Next steps

1. Map screen (lab wall map + pause menu; travel to reached rooms), then the Village Fund shop at Bram
   (shard balance, the 4 restoration projects with prices; buying calls RestorationProject.Restore).
2. Team: PixelLab Momentum Engine (empty slots + running, ~200x160) by ~Nov 24; optional map parchment background;
   music tracks by ~Nov 27.
3. Rooms 5–8 (designs to propose one at a time; Room 8 finale uses the Flywheel + Counterweight and whatever part Rooms 5–7 add).
