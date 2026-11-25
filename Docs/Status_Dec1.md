# Shards of Motion — Status & Plan to December 1, 2026

Read this first in a new session. Rules of the project are in `CLAUDE.md`; this file is the current scope,
what's done, how the tools are used, and what's next. **Last updated: Nov 25** (finale built; Heart Engine polished).

## Scope for December 1 (agreed Nov 20)

A complete, playable game:
**Title → Alderbrook village → Region 1 "The Shattered Grounds" (8 rooms) → finale → back to the village (ending, credits).**

- **Region 1: all 8 rooms.** Rooms 1–7 done. **Room 8 = the finale** ("Heart of the Forest"), built as three rooms
  (R1_Room08 / 09 / 10, one entry on the map): the machine parts complete the **Momentum Engine** and its puzzle frees
  the Great Shard. All three parts are built and playable; part 2 is fully dressed, parts 1 and 3 still graybox.
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
| Nov 24 | Room 5 (**done**) |
| Nov 25 | Room 6 (**done**) |
| Nov 26 | Room 7 (**done**) |
| Nov 27 | Room 8 finale (Momentum Engine) (**built Nov 25**: 3 parts playable; part 2 dressed; parts 1 and 3 visual passes left) |
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
- Rooms 1–5 (graybox + art): 1 First Exchange, 2 Weight of Things, 3 Together We Move (cargo ferry), 4 Broken Crossing (momentum, rubble walls),
  5 Long Push (friction: stone / wet stone / moss floors, two crates, Bearing = machine part 3).
- Room 6 Weight of Cooperation (balance lift up and over, timed west lift: SignalTimer), Room 7 Unstoppable Object
  (wet culvert, timed stop block catches the ram). Rooms 6-7 dressed. Rooms are built as visible chain reactions
  (see memory: room design direction).
- **Room 8 finale "Heart of the Forest"** (Finale > 1. Add Finale Rooms To Scene makes it three rooms; one map entry;
  light gets darker and bluer from Room 5 on: RoomMood + MoodDirector):
  - **Part 1 "The Descent"** (R1_Room08, graybox): load ore into the Heart Cart (2 -> 4 kg), chock plate + launch,
    the loaded cart breaks the root wall (42 kg·m/s), Burst it into the cart chute (saved: r1_cart_delivered).
  - **Part 2 "The Heart Engine"** (R1_Room09, **dressed**): the team's PixelLab engine (Art/Engine) at 2x; assembly
    screen (both players fit Bearing, Flywheel, Counterweight; saved r1_heart_engine_installed); a cart loop: chain lift
    -> winch hammer (P2 winds it by tethering a brass ring: 6:1 pulley + ratchet; P1 releases it from the balcony; 1D
    collision e = 0.9, ore makes the hit stronger) -> ramp -> striker -> flywheel (friction 3 kg·m/s per s). Stages:
    30 hall lamps, 60 alcove platform (shard), 100 awakening (saved r1_engine_awake, exit opens). Readability: height
    ruler, swing path, pulley label, milestone board, lanterns. Players walk past the striker over a grate on the shaft.
  - **Part 3 "The Great Shard"** (R1_Room10, graybox): lifts powered by the awakening, two pedestals, hold Interact
    together 3 s: the shard heals, region1_complete is saved, end-of-region cards (RegionFinale: one card per room and
    its lesson, Wren's words), back to the village.
- Game shell: save system (auto, one slot), title screen, pause menu, settings, dev tools.
- Title screen v2: PixelLab forest-ruin painting (Art/Title), village crystal floating in the arch, P1/P2 framing it.
- Village core: 3 screens with PixelLab buildings (adapted: Art/Village; sources in Art/Village/Source), chibi Wren/Bram/Mira
  (idle + action, NpcActivity), dialogue, VillageAmbience (birds, smoke, embers, sway, pollen, motes),
  RestorationProject states (well, lamps, garden, shrine; broken until funded). Dev menu: Village: Restore / Unrestore All.
- Map screen (lab wall map + pause menu): Region 1 path from Alderbrook, per-room shards / part / journal read, totals,
  locked regions fogged, travel to any reached room. Save v2: shard ids `<room>/<name>`, journal reads `journal_<entry>`.
- Village Fund ledger at Bram (VillageFund: well 3, lamps 5, garden 6, shrine 8; balance = collected - spent).
  Interact prompts restyled: small key-caps with player dots, shared when both players aim at the same thing (InteractPrompt).

## Tools (Tools > Shards of Motion) — typical run order

- **New room:** `Room N > 1. Build Graybox` → `Room N > 2. Apply Visual Pass` → `Journal > Draw Room Sketches`
  (Region1_ShatteredGrounds open, so raised doorways are read).
- **Art:** `Characters > 1. Build Character Sprites` (P1, P2, Wren...), `Effects > 1. Build Effect Sprites`,
  `UI Kit > 2. Build UI Kit`, `Create UI Prefabs` (canvas: journal, pause, settings).
- **Text:** `Journal > Update All Entries` (entries + tips; keeps drawn sketches).
- **Map:** `Map > Draw Region Map` (creates/updates `Data/Map/Region1_Map.asset` and draws `Art/Map`), then
  `Create UI Prefabs` (the map screen is part of UI_Canvas). Re-run both after moving markers in the asset.
- **Region 1 trail home:** `Room 1 > Add Trail Home` (gate back to the village at Room 1's west edge; re-runnable).
- **Shell:** `Game Shell > Build Title Scene`, `Game Shell > Build Village` (run Characters > 1. Build Character Sprites
  first; Build Village before Build Title Scene, which uses the village crystal).
- **Finale (Room 8):** `Finale > 1. Add Finale Rooms To Scene` (once; save the scene) → `Room 8 > 0. Import Engine Art`
  → `Room 8 > 1. Build Graybox (Part 1)` → `Room 8 Part 2 > 1. Build Graybox` → `Room 8 Part 2 > 2. Apply Visual Pass`
  → `Room 8 Part 3 > 1. Build Graybox` → `Backgrounds > 2. Apply To Rooms` → `Create UI Prefabs` (assembly screen)
  → `Journal > Update All Entries`. `Room 8 Part 2 > 0. Build Heart Engine Art` redraws its sprites (the builder makes
  missing ones itself). Re-run Part 2's visual pass after its graybox (it links the lanterns to the 30 stage).
- **Testing:** `Dev > Play From Room > Room N` (Room 8 part 2 / part 3 have their own entries); in play: PgDn/PgUp rooms,
  F9 reset, F10 hold gates. `Dev > Reset Heart Engine (save)` (outside Play) forgets "assembled" and "awake".

## Conventions

- Rooms are 20 x 11.25 m; room-local floor top y = -4.5; doorways at x = ±10 (adjustable opening height: `Doorway`).
- Room builders (`RoomNBuilder`) clone Room 1's graybox parts via `RoomBuildKit`; `RoomDresser` dresses by type;
  a short per-room `RoomNDressing` adds decoration. Everything they make is re-runnable.
- Physics values never change to fit art. Player body 0.8 x 1.5 m (scaled 1x1 collider), 1 kg. Crates 1/2/3 kg, mu = 1/3.
- Gameplay reads input only through `PlayerInputReader`; menus/dev keys may read devices directly (documented).
- Commit after the user tests and confirms; push only when asked (pushing is blocked by the permission settings).
- Third-party art lives in `Assets/ThirdParty` (credits in its CREDITS.md). Legacy Fantasy is committed; **Humble Paper UI
  is deliberately not committed** (unused so far; its license forbids redistribution).

## Next steps

1. Visual passes for finale parts 1 (The Descent) and 3 (The Great Shard), in the Heart Engine's style (Legacy Fantasy
   + Village Props, warm lanterns, cyan runes).
2. The ending in the village: nothing reacts to region1_complete yet (assembly / restoration at the lab, credits).
   The end-of-region cards already play and return to the village.
3. Lock Regions 2–3 at the road gate, sound effects, music (team: 1–2 tracks).
4. Balance check: can the Heart Engine reach 100 comfortably with 3 kg·m/s drain? (passed the Nov 25 playtest; recheck
   in the full playthrough). Then the full New Game → credits playthrough, Windows build, controller test.
