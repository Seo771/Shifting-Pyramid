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

Must not know about Player, Mummy, Treasure, Exit, or UI.

### MazeTile
- grid coordinate
- N/S/E/W wall states
- generic tile flags if required
- connection state

Avoid gameplay behavior here.

### MazeGenerator
- initial connectivity generation
- DFS / Prim
- respecting reserved/fixed tiles

Prefer operating on maze data instead of visual GameObjects.

### MazeChanger
- selecting regions
- generating candidate changes
- protecting excluded tiles
- submitting candidates for validation

### PathValidator
- BFS reachability
- required-route checks
- validation result

Should be reusable and independent from visuals.

### MazeView / MazeRenderer
- read maze data
- turn walls on/off
- sync logical state to Unity GameObjects

## 3. Game-Layer Examples

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
