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
- the player must collect a required number of curse jars
- Curse Jar Rooms are fixed, manually authored 3x3 room prefabs
- Exit is fixed and unlocks only after the curse jar requirement
- Special Rooms use manually authored 3x3 prefabs and can be replaced during runtime changes
- the Mummy is the main monster
- traps are modular game content

## Current Implementation
The current lifecycle, preservation rules, and ordered box/torch/NavMesh updates are documented in [Maze lifecycle](../Docs/MazeLifecycle.md). That document supersedes older event-order descriptions in the handoff/design documents.

The code reserves 3x3 room footprints first, generates corridors with room-aware randomized DFS, connects every declared entrance, places the exit, and validates room/exit reachability with BFS. Runtime corridor changes and validated special-room replacements are implemented.

Each Curse Jar Room or Special Room uses one prefab occupying nine grid cells. Prefabs need a root `MazeRoomView` and child `MazeRoomEntranceMarker` components. Existing single-tile room prefabs have not been automatically converted; assign authored 3x3 prefabs before playing with nonzero room counts.

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
- Both room types protect all nine cells from ordinary corridor changes. Curse Jar Rooms remain fixed; safe Special Rooms can be replaced through a separate validated step.

### Decisions Still Open
- Entrance counts for each authored prefab; currently every declared entrance is connected.
- Prefabs require zero authored root rotation and unit scale; placement supports 90-degree rotations.
- Special Room content persistence across replacement and final balance remain gameplay decisions.
- Final maze dimensions and room counts: the current asset uses 20x20 with one Curse Jar Room and one Special Room, leaving a one-cell corridor gap.

Implementation details and migration order are recorded in `../Docs/MazeArchitecturePlan.md` and `MazeSystem.md`.

## Portfolio Rule
The maze system will later become a separate public GitHub portfolio project.

During capstone development:

**Game completion first. Clean separation second. Public-library refactor later.**
