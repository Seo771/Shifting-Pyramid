# Suggested Project Architecture

This is a practical guideline, not a mandatory final class layout.

## 1. Suggested Unity Folder Structure

```text
Assets/
├─ Game/
│  ├─ Player/
│  ├─ Mummy/
│  ├─ Treasure/
│  ├─ SpecialRooms/
│  ├─ Exit/
│  ├─ Traps/
│  ├─ UI/
│  └─ GameFlow/
│
├─ Maze/
│  ├─ Core/
│  ├─ Generation/
│  ├─ RuntimeChange/
│  ├─ Validation/
│  └─ Presentation/
│
├─ Art/
├─ Audio/
└─ Scenes/
```

## 2. Suggested Responsibilities

### MazeGrid
- grid dimensions
- tile lookup
- neighbor relationships
- connectivity data
- room footprint ownership and entrance lookup using data only

Must not know about Player, Mummy, Treasure, Exit, or UI.

### MazeTile
- grid coordinate
- N/S/E/W wall states
- generic tile flags if required
- connection state

Avoid gameplay behavior here.

### MazeGenerator
- initial connectivity generation
- randomized DFS with room-aware traversal
- respecting reserved/fixed tiles
- reserve 3x3 footprints before corridor generation and treat each room as one connectivity unit
- connect corridors through declared entrances only

Prefer operating on maze data instead of visual GameObjects.

### Room Footprint / Entrance Data
- identify a room instance and its nine occupied grid cells
- store entrance local boundary cells and outward directions
- associate occupied cells with their room for traversal and protection
- initially describe each room as internally connected between all entrances

Implemented by `MazeRoom`, `MazeRoomTemplate`, and `MazeRoomEntrance` in Core. `MazeGrid` registers footprint ownership and internally connected cells. Prefab references and door scripts stay outside the calculation layer.

### MazeChanger
- selecting regions
- generating candidate changes
- protecting excluded tiles
- submitting candidates for validation

### PathValidator
- BFS reachability
- required-route checks
- validation result
- traversal through room entrances, including a start inside a room, using registered grid connections

Should be reusable and independent from visuals.

### MazeView / MazeRenderer
- read maze data
- turn walls on/off
- sync logical state to Unity GameObjects
- spawn one authored prefab per 3x3 room and skip ordinary tiles in its footprint
- translate prefab entrance markers into logical data before generation
- ensure corridor openings align with authored doorways

`MazeRoomView` reads `MazeRoomEntranceMarker` children into coordinate data before generation. The renderer places room roots at their footprint centers and tracks every cell's world position. Refresh updates ordinary corridor walls only; dynamic room removal/replacement is not implemented.

## 3. Game-Layer Examples

### Authored Room Prefabs / Doors
- Treasure Rooms and Special Rooms use a 3x3 footprint and a center-floor pivot
- room interiors, exterior walls, and doors are authored in Unity
- markers identify entrances; opening animations, locks, and interactions stay in the game layer
- every declared entrance is connected; dynamic Special Room replacement remains unimplemented

### TreasureRoomController
- treasure interaction
- collected state
- report progress to GameFlow

### ExitController
- locked/unlocked state
- treasure requirement
- escape interaction

### MummyAI
- detection
- chase
- attack
- route update after maze change

### TrapController
- warning
- activation
- damage/block
- reset

## 4. Communication
Prefer loose communication through events/data.

Good:
```text
Maze changed
→ event raised
→ Mummy AI requests new route

Treasure collected
→ GameFlow updates count
→ Exit checks unlock state
```

Avoid:
```text
MazeChanger directly manipulates MummyAI internals
MazeGenerator directly modifies Treasure UI
PathValidator depends on TreasureRoom MonoBehaviour
```

## 5. Practical Rule
If a class name contains a game concept such as:
- Mummy
- Treasure
- Pyramid
- Player
- Trap

it probably belongs outside Maze Core.

If the class makes sense in a completely different maze game, it is a good Maze Core candidate.
