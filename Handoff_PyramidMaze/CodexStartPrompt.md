# Recommended First Prompt for Codex

Read `AGENTS.md`, `MazeSystem.md`, `GameRules.md`, and `Architecture.md` before making changes.

This is a Unity 6.3 LTS 3D capstone project.

Highest-priority rules:
1. Complete the game first.
2. Keep Maze Core separate from game-specific systems.
3. Do not prematurely turn the code into a reusable library.
4. Do not hardcode the maze so tightly into the pyramid game that it cannot be extracted later.
5. After game completion, the maze system will be refactored into a public GitHub portfolio project.

For each new subsystem:
- state which layer it belongs to
- state its responsibility
- list dependencies
- point out coupling that could make future maze extraction difficult

Do not perform large refactors without asking first.

The first implementation target is the grid/tile representation for the dynamic 3D maze.
