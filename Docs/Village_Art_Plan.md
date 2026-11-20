# Alderbrook: Village Art Plan (approved Nov 20, 2026)

Reference: the two-screen concept (Wren's Lab / Market Square). The concept is a **reference only**. Every
asset is redrawn at our scale; nothing is cut from the concept image.

Wren Alder is **he/him**.

## Revision (Nov 20): PixelLab buildings, three screens
The code-drawn building kit was dropped as too stiff. The buildings are PixelLab pixel art (sources in
`Assets/_Project/Art/Village/Source/`), adapted by a script into `Assets/_Project/Art/Village/`:
- colours nudged toward Village Props;
- baked-in smoke removed (animated smoke instead);
- the lab sign repainted from a potion flask to Wren's spiral;
- each building split into a base (lights off) and a `_Lights` overlay;
- `Cottage_Wilted` overlay, the shrine crystal cut out (lit and dim), `Shrine_Overgrown` vines, `Well_Broken`.

The real buildings are wider than planned (forge 7.4 m, shrine 4.4 m, cottage 5.5 m), so the village has **three screens**.
The order is unchanged. Room-local x positions:

| Screen | Objects (x positions) |
|---|---|
| **Wren's Lab** (x 0) | Lab −5.3, Wren −0.6, wall map 1.7, research table 4.2, lamp 8.2 |
| **Market Square** (x 20) | Cottage −7.0, Mira −3.6, lamp −2.6, board −1.2, fruit stall 1.4, shrine 5.0, well 8.4 |
| **Forge & East Road** (x 40) | Forge −3.6, Bram −4.4 (at the hearth), lamp 2.2, road sign 5.6, region gate 8.3 |

The tables below are the original plan. Where they differ from this revision, the revision wins.

## Scale and rules
- Each screen is 20 × 11.25 m (640 × 360 px at 32 PPU); the floor top is at y = −4.5. Point filtering, soft-shaded pixel art.
- Characters are 60 px tall (1.9 m), so doors are about 66 px tall.
- Building heights:

| Building | Height | Pixels |
|---|---|---|
| Mira's cottage | about 4.6 m | 148 px |
| Bram's forge | about 5.2 m | 166 px |
| Wren's lab house | about 5.5 m | 176 px |
| Lab observatory dome top | about 7.5 m | 240 px |

- Village Props pack sprites are reused wherever possible; custom pieces only where the pack has nothing.
- Colliders, room sizes and doorways are unchanged. The floor stays a clear, flat walking strip.

### Sorting layers (existing; no project-settings change)

| Layer | Contents |
|---|---|
| Background | Sky |
| BackDecor | Darkened trees |
| Midground (low order) | Buildings |
| Props | Tables, stall, well, anvil |
| Player | NPCs and players |
| Foreground | Grass tufts and framing leaves |

## Pack sprites reused

| Concept item | Village Props sprite(s) |
|---|---|
| Wall lanterns on buildings | Road Lamp Support + Road Lamp Light On/Off |
| Lamp posts | Road Lamp Pillar + Light On/Off |
| Banners | Banner + Banner Pillar, recoloured blue, red and green, with custom emblems on top |
| Research table, chair | Table, Chair |
| Instruments on the table | White Bottle, Wine Bottle, Cup, Kettle, Crate Small |
| Wall map board | Billboard + Billboard Noti 01–04 as pinned notes |
| Notice board | Billboard + Noti 01–04 |
| Fruit stall | Stall Table, Stall Tent, Stall Tent Pillar, Stall Fruit 01–04, Basket, Apple, Gunny Bag 01/02 |
| Well | Well, Bucket |
| Forge yard | Anvil, Fire Bowl, TX FX Flame (animated), Weapon Rack, Hammer, Axe, Sword, Cauldron, Wood Logs, Wheelbarrow Wheel |
| Stone base under buildings | Brick Wall |
| Fences | Fence 01 pillars and rails |
| Barrels, crates | Barrel, Crate Large, Crate Small |
| Plants | Pot 01–03, Flower 01/02, Sunflower 01–03, Bush 01–03, Grass 01–09 |
| Trees, rocks | Tree 01/02, Rock 01–03, Stump |
| Ruins road sign | Road Sign 01/02 + Road Sign Text 01–03 |
| Chimney smoke | The existing FX dot |

Not used:
- **Stone of Recall:** it's the reset shrine everywhere else, so using it here would confuse players.
- **Statue:** it doesn't match the crystal-shrine idea.

## Buildings (one shared kit)
The kit uses slate-blue roof, cream plaster, dark timber and warm stone. Each building is a group of separate
sprites, so broken and repaired versions can swap individual pieces.

| Building | Pieces |
|---|---|
| **Wren's Lab** (about 7.6 m wide) | Brick Wall base; plaster-and-timber wall; slate gable roof with mossy edge; arched door; 2 lit windows plus an attic window; chimney; stone tower (2.8 m wide) with dome and telescope; blue spiral banner on the tower; 2 wall lanterns; vines. A lean-to research porch to the east, sheltering the map board and table. |
| **Mira's cottage** (about 4.2 m wide) | The same wall kit with a red clay roof; door; 2 windows with flower boxes; chimney; climbing flowers; wall lantern. |
| **Bram's forge** (about 4.6 m wide) | Stone walls; slate roof; tall chimney; open stone hearth arch with the animated flame and a glow light; open-sided timber workshop roof; red banner with crossed hammers; wall lantern. |
| **Shard shrine** (about 3.4 m wide) | Ruined stone arch with moss; 3-step plinth; spiral stone; large crystal (dim and glowing versions); green banners on the pillars. |

## Layout (x positions in metres; each screen runs from −10 to +10)

### Wren's Lab (the west screen, where a new game starts)

| x | Object | Notes |
|---|---|---|
| −9.2 to −1.6 | Lab building | Players spawn in front of the door |
| −0.8 | **Wren** (talk) | Faces west, holding the book |
| −0.4 to 5.2 | Research porch | |
| 2.0 | **Wall map** | Map screen |
| 3.6 | Research table | Parts display, books, astrolabe, shard sample |
| 4.8 | Chair | |
| 5.8 to 8 | Barrels, crate, fence | |
| 7.4 | Lamp post with a hanging banner | |
| 9 | Big tree framing the exit to the square | |

### Market Square (west to east, in this order)

| x | Object | Notes |
|---|---|---|
| −9.6 to −5.4 | Mira's cottage | |
| −6.0 | **Mira** (talk) | At her flower box |
| −3.6 | **Notice board** (read) | |
| −1.4 | Fruit stall | |
| 1.6 | Shard shrine | |
| 4.4 | Well | |
| 5.6 to 10 | Bram's forge | |
| 6.4 | **Bram** (talk; the Village Fund opens from him) | |
| 7.3 | Anvil | |
| 9.0 | **Ruins road**: RegionGate + Road Sign | The Region 2 and 3 "road closed" boards hang on the same post |
| −4.8, 5.4 | Lamp posts | |

- Interaction points are at least 2.4 m apart.
- The screen-edge stone walls keep their colliders but are dressed as tree trunks and bushes.

## Restoration projects (modular states)
`RestorationProject` (an id, plus a `Broken` and a `Restored` child group) reads the save event `fund_<id>`,
switches the groups on load, and plays a short sparkle when a project is funded.

| Project | Broken | Restored |
|---|---|---|
| Repair the well | Gap in the roof, darker wood, rope missing, bucket tipped on the ground | Clean well, bucket on the rope, occasional water drip |
| Relight the lamps | Light Off sprites, lights off | Light On sprites with warm flickering lights; covers the posts and wall lanterns on both screens |
| Restore the shard shrine | Grey crystal, cracked plinth, overgrown vines | Glowing crystal that bobs, cyan light, drifting motes, flowers |
| Mend Mira's garden and the fences | Fallen fence rails, empty flower boxes, tarp patch on the forge roof | Fences upright, flowers in bloom, roof patched |

Suggested costs: 3 / 5 / 6 / 8 shards (22 of the 24 in Region 1). To be confirmed in the shop step.

## Custom assets to make
1. Building kit: wall tiles (plaster-and-timber, stone); slate and red roof tiles with ridge and gable ends; arched door; windows (lit and unlit); attic window; chimney.
2. Observatory tower, dome and telescope.
3. Porch roof and the forge's open workshop roof.
4. Forge hearth arch.
5. Vines and moss overlays.
6. Flower boxes, plus a wilted-flowers recolour.
7. Banner emblems (spiral, hammers, leaf) and the banner recolours.
8. Bookshelf, book stacks, astrolabe, map parchment.
9. Shrine: arch, plinth, large crystal (2 states).
10. Broken states: well copy with roof damage, tarp patch. Fallen fences reuse pack rails at an angle.
11. Wren, Bram and Mira in chibi style (48×60): idle plus one action each (reading, hammering, watering).

## Order of work
1. Building kit, with a preview image of each building.
2. Characters.
3. Assembly: VillageBuilder layout, edge-wall dressing, region gate moved to x 9.0, barricade removed.
4. RestorationProject plus the NPC idle/action component.

If time runs short, cut the action animations first, then the observatory details.
