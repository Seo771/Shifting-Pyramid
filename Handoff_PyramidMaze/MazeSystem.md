# Dynamic Maze System Specification

## 1. Overview
The game uses a 3D grid-based maze inside a pyramid.

The maze is generated at game start and partially changes during gameplay.

Physical grid positions do not move. Ordinary corridor tiles change their directional wall states. Treasure Rooms and Special Rooms will use manually authored 3x3 prefabs.

Status: room-aware generation and rendering are implemented. Authored 3x3 prefabs must be assigned to the new settings fields. Runtime maze changes are still unimplemented. See `../Docs/RoomPrefabSetup.md` for setup.

## 2. Tile Model
Each logical tile occupies one grid coordinate.

Each tile stores four-direction connectivity:
- North
- South
- East
- West

Conceptual data:
- GridPosition
- NorthWall
- SouthWall
- EastWall
- WestWall
- TileCategory
- IsProtected

Exact code structure is not finalized.

## 3. Unity 3D Representation
Recommended ordinary corridor tile hierarchy:

```text
MazeTileObject
├─ Floor
├─ Corners / Pillars
├─ Wall_North
├─ Wall_South
├─ Wall_East
└─ Wall_West
```

Fixed:
- Floor
- Corners / Pillars

Dynamic:
- N/S/E/W walls

### 3x3 Room Representation
One Treasure Room or Special Room prefab occupies nine grid cells. Its floor, walls, doors, and interior are authored directly in Unity; ordinary tile prefabs are not spawned inside its footprint.

Use a center-floor pivot and a footprint of `3 * tileSize` on each horizontal axis (30x30 with the current tile size of 10).

Each entrance has a marker and logical data containing a local boundary cell and outward direction. Local coordinates run from `(0,0)` at the southwest cell to `(2,2)` at the northeast cell. The north-center entrance is `(1,2), North`. Markers must align with the shared boundary and the neighboring corridor's opening.

Maze generation receives coordinate/connectivity data, not Transform or door GameObject references. Physical door behavior belongs to the game layer.

The initial design assumes all room entrances are mutually reachable inside the prefab. Internal partitions that break this assumption require additional connectivity data. Locked doors must be accounted for according to the validation scenario; logical connectivity alone does not prove a currently locked door is passable.

## 4. Neighbor Consistency
Adjacent tiles must agree on shared walls.

Example:
A east opening == B west opening.

One-sided openings are invalid.

## 5. Initial Maze Generation
Implemented 3x3-room flow:
1. Reserve non-overlapping room footprints and declare entrances. Exclude the start cell and leave usable corridor space outside entrances.
2. Build connectivity with ordinary cells and whole rooms as traversal units. Room interiors are authored spaces, not nine independent DFS carving targets.
3. Connect corridors only through permitted room entrances. Keep room walls closed elsewhere and synchronize the neighboring corridor wall.
4. Choose a reachable edge cell outside room footprints for the existing exit.
5. Validate corridor and room connectivity, required objectives, and entrance alignment.
6. Spawn one prefab per room and ordinary tile prefabs only outside room footprints.

DFS treats each room center as one visited node and traverses its declared entrances. After carving, every authored entrance is opened. Multiple entrances can create loops, so the combined map is not required to remain a perfect maze.

Room fitting is a geometric constraint, not just a count of available cells. Rooms are placed inside the outer corridor boundary with at least one ordinary cell between footprints. Placement retries up to 128 times and reports a configuration error on failure. The current asset uses 15x15 with five Treasure Rooms and two Special Rooms. Rotation and non-unit room root scale are not supported.

## 6. Runtime Maze Change Candidates

### Method A: Single 3x3 Region
- choose one 3x3 region
- regenerate internal wall connectivity
- preserve protected elements
- validate
- apply only if valid

Pros:
- stable
- easy to debug
- lower player confusion

Risk:
- may be too subtle on a large map

### Method B: Multiple 3x3 Regions
- choose multiple separated 3x3 regions
- regenerate all selected regions
- validate the whole maze
- apply only if valid

Pros:
- more noticeable change
- stronger gameplay impact

Risk:
- more player confusion
- more validation complexity

Both methods remain active candidates. Final choice should come from playtesting.

## 7. Region Protection Rules
Avoid changing:
- player current tile
- player safety radius
- entire Treasure Room footprints and their entrance connections
- Exit
- entire fixed Special Room footprints and their entrance connections, if a fixed-room policy is selected
- mummy current tile / safety radius
- other protected critical tiles

Currently both Treasure Room and Special Room footprints are marked protected at initial generation. The future changer must also preserve the required entrance connections.

Exact protection radius is not finalized.

The 3x3 regeneration region and the 3x3 room footprint are different concepts. Do not partially regenerate an authored room. Dynamic Special Room appearance/removal and protection rules remain undecided; moving or replacing a room requires validating the full footprint and external connections.

## 8. External Connections of Changed Region
When regenerating a 3x3 region:

1. Select region.
2. Record region boundary connections to outside tiles.
3. Regenerate internal connectivity.
4. Restore required external connection ports.
5. Validate global paths.
6. Apply only if valid.

## 9. Path Validation
Recommended: BFS.

Current BFS operates on tile wall data. Room registration opens internal grid connections under the all-interior-cells-connected assumption, so BFS also supports starts inside a room. Only declared boundary entrances can be carved. For treasure objectives, reaching a room assumes its treasure interaction point is accessible. Physical prefab walls/colliders must match this assumption; locked-door state is not yet modeled.

A* may be used where useful, but BFS is enough for reachability checks.

Minimum validation:
- Player -> Exit path exists
- Player -> all remaining required Treasure Rooms are reachable
- Player is not trapped
- Mummy is not completely isolated

If validation fails:
- reject candidate change
- rollback
- generate another candidate

## 10. Maze Change Flow

```text
Timer expires
→ Select change region(s)
→ Exclude protected areas
→ Generate candidate wall states
→ Check neighbor consistency
→ Run BFS validation
→ If invalid: discard/regenerate
→ If valid: apply walls
→ Update navigation/pathfinding
→ Resume gameplay
```

## 11. Presentation Feedback
Before a maze shift, the Game Layer may use:
- stone movement sound
- vibration
- dust
- torch flicker
- camera shake
- warning timer

Do not embed these presentation features inside the Maze Core.

## 12. Future Portfolio Refactor
After game completion, possible public-package features:
- DFS / Prim selector
- deterministic Seed generation
- configurable grid size
- configurable region size
- single/multi region change modes
- protected-tile API
- path validation API
- runtime debug visualization
- sample scene
- Unity Package Manager-ready structure
