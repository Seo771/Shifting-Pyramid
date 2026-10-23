# Game Rules

## 1. Theme
- 3D pyramid exploration game
- desert / ancient pyramid theme
- mummy monster
- Curse Jar Rooms
- Special Rooms
- traps
- Exit
- maze changes during play

## 2. Main Objective
The player must collect a required number of curse jars before escaping.

```text
Curse Jar Count < Required Count
→ Exit locked

Curse Jar Count >= Required Count
→ Exit active

Player reaches active Exit
→ Escape success
```

Exact required curseJar count is not fixed yet.

## 3. Curse Jar Rooms
Curse Jar Rooms are core objective rooms.

Rules:
- N Curse Jar Rooms are generated at initial map creation.
- Representation: one manually authored 3x3 prefab per room, occupying nine grid cells. Use MazeRoomView and declared entrance markers.
- Their positions are fixed for the whole run.
- Their structure never changes.
- Their entire footprints and entrance connections are protected from maze changes.
- Collected curseJar contributes to the escape requirement.
- They must remain reachable after every maze change.

## 4. Special Rooms
Special Rooms are separate from Curse Jar Rooms.

Their representation is also a manually authored 3x3 prefab with declared entrances. Room count means whole rooms, not occupied cells. Authors choose entrance count; the generator connects every declared entrance.

Current code places Special Rooms during initial generation and protects all nine cells. Runtime appearance/removal is not implemented. Fixed rooms are the current behavior; the dynamic variant below remains a future candidate.

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
- during maze changes, a suitable 3x3 ordinary-cell footprint may become a temporary Special Room

Restrictions:
- reserve the entire non-overlapping 3x3 footprint and reconnect only through declared entrances
- validate reachability when the room appears or is removed; never partially replace its footprint
- never replace Curse Jar Room
- never replace Exit
- never replace player tile
- never replace fixed Special Room

This feature is not finalized. Keep the architecture open to it, but do not overbuild it now.

## 5. Exit
- fixed position
- does not move
- may be discovered before it is usable
- locked until curse jar requirement is met
- must remain reachable after maze changes
- paths leading to it may change

## 6. Player
- explores
- finds Curse Jar Rooms
- collects required curseJar
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
- avoid critical Exit/CurseJar conflicts

Traps belong to Game Layer.

## 9. Main Loop

```text
Enter Pyramid
→ Explore Maze
→ Find Curse Jar Rooms
→ Collect required curseJar
→ Maze periodically changes
→ Avoid Mummy and traps
→ Curse jar requirement completed
→ Exit becomes active
→ Reach Exit
→ Escape
```
