# AGENTS.md

## Project: Pyramid Dynamic Maze Capstone

Codex MUST follow these rules when modifying or generating code.

# 1. Highest-Priority Rules

## 1.1 Game completion first
Do not over-engineer the project into a generic framework during the capstone phase.

Avoid:
- premature library/package architecture
- unnecessary abstraction
- excessive interfaces or dependency injection
- complex editor tooling unless clearly needed

Prefer:
- practical implementation speed
- readable, maintainable code
- small focused classes

## 1.2 Maze Core and Game Layer MUST stay separated
The maze algorithm will later be extracted, refactored, documented, and released publicly as a portfolio project.

Maze Core examples:
- MazeGrid
- MazeTile
- MazeGenerator
- MazeChanger
- PathValidator
- maze connectivity data
- wall-state data
- region regeneration
- path validation

Game Layer examples:
- Player
- Mummy
- TreasureRoom
- SpecialRoom
- Exit
- Trap
- UI
- GameFlow
- sound / VFX

Maze Core must not directly depend on Player, Mummy, TreasureRoom, Exit, Trap, or UI.

## 1.3 Portfolio refactoring happens AFTER game completion
After the game demo is finished:
- remove game-specific dependencies
- improve public APIs
- add Seed-based deterministic generation
- add sample scenes
- add debug visualization
- add documentation / README / GIFs
- package as a reusable Unity package
- publish publicly on GitHub

Required balance:

**Do not build the public library now. Do not hardcode the maze so deeply that extraction becomes difficult later.**

# 2. Technology
- Unity 6.3 LTS
- Universal 3D / URP
- C#
- PC
- 4-person capstone team
- 1 main/core programmer, 3 Unity-beginner support members

# 3. Coding Expectations
Prefer:
- clear names
- one main responsibility per class
- composition over deep inheritance
- data-driven configuration where useful
- minimal hidden dependencies
- no giant manager classes
- no magic numbers

Do not perform a large refactor unless explicitly requested.

# 4. Current Priority
1. Tile/grid representation
2. Initial maze generation
3. 3D wall representation
4. Path validation
5. Runtime maze change
6. Treasure rooms / exit condition
7. Player
8. Mummy AI
9. Special rooms
10. Traps
11. Polish
12. Portfolio refactor after game completion

# 5. Important Maze Constraint
The maze is not rebuilt by swapping entire tile prefabs.

Each tile uses:
- fixed Floor
- fixed Corner/Pillar
- North Wall
- South Wall
- East Wall
- West Wall

The four directional walls are enabled/disabled to represent connectivity.

# 6. Before Writing Code
Before implementing a maze feature:
1. Decide whether it belongs to Maze Core or Game Layer.
2. Keep game objects out of Maze Core whenever possible.
3. Prefer grid/data references over gameplay GameObject references inside algorithms.
4. Keep path validation independent from visuals.
5. Preserve future extraction of Maze Core.
