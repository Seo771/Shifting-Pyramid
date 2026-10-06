# Codex Handoff - Pyramid Dynamic Maze

## Read order
1. `AGENTS.md`
2. `MazeSystem.md`
3. `GameRules.md`
4. `Architecture.md`
5. `CodexStartPrompt.md`

## Current Direction
This capstone is a 3D pyramid maze game where:
- the maze is grid/tile based
- tile positions remain fixed
- ordinary corridor tiles keep fixed floors and corners/pillars
- ordinary corridor N/E/S/W walls are enabled/disabled
- selected maze regions change periodically
- changes are path-validated
- the player must collect a required number of treasures
- Treasure Rooms are fixed, manually authored 3x3 room prefabs
- Exit is fixed and unlocks only after the treasure requirement
- Special Rooms also use manually authored 3x3 room prefabs; their runtime appearance policy is not finalized
- the Mummy is the main monster
- traps are modular game content

## Current Implementation
The code reserves 3x3 room footprints first, generates corridors with room-aware randomized DFS, connects every declared entrance, places the exit, and validates room/exit reachability with BFS. Runtime maze changes are not implemented yet.

Each Treasure Room or Special Room uses one prefab occupying nine grid cells. Prefabs need a root `MazeRoomView` and child `MazeRoomEntranceMarker` components. Existing single-tile room prefabs have not been automatically converted; assign authored 3x3 prefabs before playing with nonzero room counts.

Setup instructions: [3x3 room prefab setup](../Docs/RoomPrefabSetup.md).

### 3x3 Room Prefab Direction
- Authors build the room interior, exterior walls, and doors directly in Unity.
- Keep the existing grid scale. With a tile size of 10, a room occupies 30x30 world units; use a center-floor pivot.
- Reserve non-overlapping room footprints before generating corridors. Do not spawn ordinary tile prefabs in the nine occupied cells.
- Mark each entrance with its local boundary cell and outward N/E/S/W direction. For example, local cell `(1,2)` facing North is a north-center entrance, using `(0,0)` as the southwest footprint cell.
- Treat each room as one connectivity unit during generation and connect corridors only through declared entrances.
- Match the neighboring corridor opening to the room entrance; do not carve through an unmarked room wall.
- Keep entrance coordinates and connectivity in maze data; door movement, locking, and visuals belong to the game/presentation layers.
- Initially assume all entrances within a room are mutually reachable. An internally partitioned room requires explicit internal connectivity data.
- Both room types currently remain fixed and have all nine cells marked protected. Future maze changes must preserve required entrance connections. Special Room relocation/replacement is not implemented.

### Decisions Still Open
- Entrance counts for each authored prefab; currently every declared entrance is connected.
- Future prefab rotation support; currently room roots require zero rotation and unit scale.
- Special Room lifetime/protection policy.
- Final maze dimensions and room counts: the current asset uses 15x15 with five Treasure Rooms and two Special Rooms, leaving a one-cell corridor gap.

Implementation details and migration order are recorded in `../Docs/MazeArchitecturePlan.md` and `MazeSystem.md`.

## Portfolio Rule
The maze system will later become a separate public GitHub portfolio project.

During capstone development:

**Game completion first. Clean separation second. Public-library refactor later.**
