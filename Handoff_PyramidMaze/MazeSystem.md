# Dynamic Maze System Specification

## 1. Overview
The game uses a 3D grid-based maze inside a pyramid.

The maze is generated at game start and partially changes during gameplay.

Physical tile positions do not move. Each tile changes its directional wall states.

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
Recommended tile hierarchy:

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

## 4. Neighbor Consistency
Adjacent tiles must agree on shared walls.

Example:
A east opening == B west opening.

One-sided openings are invalid.

## 5. Initial Maze Generation
Current candidates:
- Randomized DFS
- Randomized Prim

Final choice is not fixed yet.

Initial flow:
1. Generate connected maze.
2. Reserve required fixed rooms.
3. Apply connectivity to tile data.
4. Run path validation.
5. Present the maze only if valid.

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
- Treasure Rooms
- Exit
- fixed Special Rooms
- mummy current tile / safety radius
- other protected critical tiles

Exact protection radius is not finalized.

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
