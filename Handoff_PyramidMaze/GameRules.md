# Game Rules

## 1. Theme
- 3D pyramid exploration game
- desert / ancient pyramid theme
- mummy monster
- Treasure Rooms
- Special Rooms
- traps
- Exit
- maze changes during play

## 2. Main Objective
The player must collect a required number of treasures before escaping.

```text
Treasure Count < Required Count
→ Exit locked

Treasure Count >= Required Count
→ Exit active

Player reaches active Exit
→ Escape success
```

Exact required treasure count is not fixed yet.

## 3. Treasure Rooms
Treasure Rooms are core objective rooms.

Rules:
- N Treasure Rooms are generated at initial map creation.
- Their positions are fixed for the whole run.
- Their structure never changes.
- They are excluded from maze-change regions.
- Collected treasure contributes to the escape requirement.
- They must remain reachable after every maze change.

## 4. Special Rooms
Special Rooms are separate from Treasure Rooms.

### Fixed Special Rooms
Current idea:
- N rooms may be placed during initial generation.
- They remain fixed.
- They can act as landmarks.

Possible functions:
- healing
- buffs
- mummy seal
- trap control
- shortcut
- utility interaction

Exact types are not finalized.

### Dynamic Special Rooms
Candidate feature:
- during maze changes, a normal room may become a temporary Special Room

Restrictions:
- never replace Treasure Room
- never replace Exit
- never replace player tile
- never replace fixed Special Room

This feature is not finalized. Keep the architecture open to it, but do not overbuild it now.

## 5. Exit
- fixed position
- does not move
- may be discovered before it is usable
- locked until treasure requirement is met
- must remain reachable after maze changes
- paths leading to it may change

## 6. Player
- explores
- finds Treasure Rooms
- collects required treasure
- avoids mummy and traps
- survives maze changes
- escapes through Exit

Player current tile and nearby tiles should be protected from sudden maze changes.

## 7. Mummy
Current AI concept:

```text
Patrol
→ Detect
→ Chase
→ Attack
→ Search / Return
```

Possible detection:
- line of sight
- sound
- proximity

After maze change:
- invalidate current route
- calculate a new route

Pathfinding is not finalized.

Candidates:
- Unity NavMesh
- Grid A*

Do not tightly couple Maze Core to either one yet.

## 8. Traps
Current trap ideas:
- spikes
- falling rocks
- stone-door / route-blocking trap

Trap content may change or expand.

Principles:
- avoid unavoidable damage
- provide warning where appropriate
- avoid unfair spawn positions
- do not place at start
- avoid critical Exit/Treasure conflicts

Traps belong to Game Layer.

## 9. Main Loop

```text
Enter Pyramid
→ Explore Maze
→ Find Treasure Rooms
→ Collect required treasure
→ Maze periodically changes
→ Avoid Mummy and traps
→ Treasure requirement completed
→ Exit becomes active
→ Reach Exit
→ Escape
```
