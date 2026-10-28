using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Validation;

namespace ShiftingPyramid.Maze.RuntimeChange
{
    /// <summary>특수방 하나를 제거하고 새 종류의 3x3 방을 검증 후 생성한다.</summary>
    public class SpecialRoomReplacer
    {
        private const int MaxCandidateAttempts = 256;
        private static readonly MazeDirection[] Directions =
        {
            MazeDirection.North, MazeDirection.East, MazeDirection.South, MazeDirection.West
        };

        public bool TryReplace(MazeGrid grid, IReadOnlyList<MazeRoomTemplate> catalog,
            MazeCoordinate player, int playerSafeRadius, MazeCoordinate? mummy, int mummySafeRadius,
            out MazeRoom removedRoom, out MazeRoom addedRoom, int? seed = null)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (!grid.Contains(player)) throw new ArgumentOutOfRangeException(nameof(player));
            if (mummy.HasValue && !grid.Contains(mummy.Value)) throw new ArgumentOutOfRangeException(nameof(mummy));
            if (playerSafeRadius < 0) throw new ArgumentOutOfRangeException(nameof(playerSafeRadius));
            if (mummySafeRadius < 0) throw new ArgumentOutOfRangeException(nameof(mummySafeRadius));

            removedRoom = null;
            addedRoom = null;
            var specialIndexes = new List<int>();
            for (var i = 0; i < catalog.Count; i++)
                if (catalog[i] != null && catalog[i].TileType == MazeTileType.SpecialRoom)
                    specialIndexes.Add(i);
            if (specialIndexes.Count == 0) return false;

            var removable = new List<MazeRoom>();
            foreach (var room in grid.Rooms)
                if (room.Template.TileType == MazeTileType.SpecialRoom
                    && IsSafeRoom(room, player, playerSafeRadius, mummy, mummySafeRadius))
                    removable.Add(room);
            if (removable.Count == 0) return false;

            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            Shuffle(removable, random);
            var validator = new MazePathValidator();
            var allCells = new List<MazeCoordinate>();
            foreach (var tile in grid.Tiles) allCells.Add(tile.Coordinate);

            foreach (var oldRoom in removable)
            {
                var candidates = GetCandidates(grid, oldRoom, catalog, specialIndexes,
                    player, playerSafeRadius, mummy, mummySafeRadius);
                Shuffle(candidates, random);
                // 종류가 둘 이상이면 다른 프리팹을 우선 시도한다.
                for (var pass = 0; pass < 2; pass++)
                {
                    var attempts = 0;
                    foreach (var candidate in candidates)
                    {
                        if ((candidate.TemplateIndex == oldRoom.TemplateIndex) != (pass == 1)) continue;
                        if (attempts++ >= MaxCandidateAttempts) break;

                        var proposed = grid.Clone();
                        proposed.RemoveRoom(oldRoom);
                        CloseFootprint(proposed, candidate.Room);
                        proposed.AddRoom(candidate.Room);
                        foreach (var entrance in candidate.Room.Entrances)
                            proposed.SetConnection(candidate.Room.Origin + entrance.LocalCell,
                                entrance.Direction, true);

                        CarveFormerRoom(proposed, oldRoom, random);
                        ConnectFormerRoomBoundary(proposed, oldRoom, random);
                        if (!validator.AreReachable(proposed, player, allCells)) continue;

                        grid.CopyFrom(proposed);
                        removedRoom = oldRoom;
                        addedRoom = candidate.Room;
                        return true;
                    }
                }
            }
            return false;
        }

        private static List<(MazeRoom Room, int TemplateIndex)> GetCandidates(MazeGrid grid,
            MazeRoom oldRoom, IReadOnlyList<MazeRoomTemplate> catalog, List<int> specialIndexes,
            MazeCoordinate player, int playerRadius, MazeCoordinate? mummy, int mummyRadius)
        {
            var candidates = new List<(MazeRoom Room, int TemplateIndex)>();
            for (var x = 1; x + MazeRoom.Size < grid.Width; x++)
                for (var y = 1; y + MazeRoom.Size < grid.Height; y++)
                {
                    var origin = new MazeCoordinate(x, y);
                    if (!CanUseFootprint(grid, oldRoom, origin, player, playerRadius, mummy, mummyRadius))
                        continue;
                    foreach (var index in specialIndexes)
                        for (var turn = 0; turn < 4; turn++)
                        {
                            var room = new MazeRoom(origin, catalog[index], index, turn);
                            if (EntrancesHaveCorridors(grid, room)) candidates.Add((room, index));
                        }
                }
            return candidates;
        }

        private static bool CanUseFootprint(MazeGrid grid, MazeRoom oldRoom, MazeCoordinate origin,
            MazeCoordinate player, int playerRadius, MazeCoordinate? mummy, int mummyRadius)
        {
            for (var x = origin.X; x < origin.X + MazeRoom.Size; x++)
                for (var y = origin.Y; y < origin.Y + MazeRoom.Size; y++)
                {
                    var cell = new MazeCoordinate(x, y);
                    var tile = grid.GetTile(cell);
                    if (tile.IsProtected || tile.TileType != MazeTileType.Normal
                        || cell.Equals(MazeCoordinate.Zero)
                        || IsInSafeArea(cell, player, playerRadius + 1)
                        || mummy.HasValue && IsInSafeArea(cell, mummy.Value, mummyRadius + 1))
                        return false;
                }

            foreach (var room in grid.Rooms)
            {
                if (ReferenceEquals(room, oldRoom)) continue;
                var separated = origin.X >= room.Origin.X + MazeRoom.Size + 1
                    || room.Origin.X >= origin.X + MazeRoom.Size + 1
                    || origin.Y >= room.Origin.Y + MazeRoom.Size + 1
                    || room.Origin.Y >= origin.Y + MazeRoom.Size + 1;
                if (!separated) return false;
            }
            return true;
        }

        private static bool EntrancesHaveCorridors(MazeGrid grid, MazeRoom room)
        {
            foreach (var entrance in room.Entrances)
            {
                var outside = room.Origin + entrance.LocalCell + entrance.Direction.ToOffset();
                if (!grid.Contains(outside)) return false;
                var tile = grid.GetTile(outside);
                if (tile.TileType != MazeTileType.Normal || tile.IsProtected) return false;
            }
            return true;
        }

        private static bool IsSafeRoom(MazeRoom room, MazeCoordinate player, int playerRadius,
            MazeCoordinate? mummy, int mummyRadius)
        {
            foreach (var cell in room.Cells)
                if (IsInSafeArea(cell, player, playerRadius + 1)
                    || mummy.HasValue && IsInSafeArea(cell, mummy.Value, mummyRadius + 1))
                    return false;
            return true;
        }

        private static bool IsInSafeArea(MazeCoordinate cell, MazeCoordinate center, int radius)
        {
            return Math.Abs(cell.X - center.X) <= radius && Math.Abs(cell.Y - center.Y) <= radius;
        }

        private static void CloseFootprint(MazeGrid grid, MazeRoom room)
        {
            foreach (var cell in room.Cells)
                foreach (var direction in Directions)
                    grid.SetConnection(cell, direction, false);
        }

        private static void CarveFormerRoom(MazeGrid grid, MazeRoom oldRoom, Random random)
        {
            var visited = new HashSet<MazeCoordinate> { oldRoom.Center };
            var stack = new Stack<MazeCoordinate>();
            stack.Push(oldRoom.Center);
            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var options = new List<MazeDirection>();
                foreach (var direction in Directions)
                {
                    var next = current + direction.ToOffset();
                    if (oldRoom.Contains(next) && !visited.Contains(next)) options.Add(direction);
                }
                if (options.Count == 0)
                {
                    stack.Pop();
                    continue;
                }
                var selected = options[random.Next(options.Count)];
                grid.SetConnection(current, selected, true);
                var neighbor = current + selected.ToOffset();
                visited.Add(neighbor);
                stack.Push(neighbor);
            }
        }

        private static void ConnectFormerRoomBoundary(MazeGrid grid, MazeRoom oldRoom, Random random)
        {
            var edges = new List<(MazeCoordinate Cell, MazeDirection Direction)>();
            foreach (var cell in oldRoom.Cells)
                foreach (var direction in Directions)
                {
                    var neighbor = cell + direction.ToOffset();
                    if (oldRoom.Contains(neighbor) || !grid.Contains(neighbor)) continue;
                    var tile = grid.GetTile(neighbor);
                    if (tile.TileType == MazeTileType.Normal && !tile.IsProtected)
                        edges.Add((cell, direction));
                }
            Shuffle(edges, random);
            var components = GetComponents(grid);
            var parents = new int[grid.Width * grid.Height];
            for (var i = 0; i < parents.Length; i++) parents[i] = i;
            foreach (var edge in edges)
            {
                var neighbor = edge.Cell + edge.Direction.ToOffset();
                var cellComponent = Find(parents, components[edge.Cell]);
                var neighborComponent = Find(parents, components[neighbor]);
                if (cellComponent != neighborComponent)
                {
                    grid.SetConnection(edge.Cell, edge.Direction, true);
                    parents[cellComponent] = neighborComponent;
                }
            }
        }

        private static int Find(int[] parents, int index)
        {
            while (parents[index] != index)
            {
                parents[index] = parents[parents[index]];
                index = parents[index];
            }
            return index;
        }

        private static Dictionary<MazeCoordinate, int> GetComponents(MazeGrid grid)
        {
            var components = new Dictionary<MazeCoordinate, int>();
            var queue = new Queue<MazeCoordinate>();
            var count = 0;
            foreach (var tile in grid.Tiles)
            {
                if (components.ContainsKey(tile.Coordinate)) continue;
                components.Add(tile.Coordinate, count);
                queue.Enqueue(tile.Coordinate);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var direction in Directions)
                    {
                        var next = current + direction.ToOffset();
                        if (!grid.Contains(next) || components.ContainsKey(next)
                            || !grid.GetTile(current).IsOpen(direction)
                            || !grid.GetTile(next).IsOpen(direction.Opposite())) continue;
                        components.Add(next, count);
                        queue.Enqueue(next);
                    }
                }
                count++;
            }
            return components;
        }

        private static void Shuffle<T>(List<T> values, Random random)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var selected = random.Next(i + 1);
                var swap = values[i];
                values[i] = values[selected];
                values[selected] = swap;
            }
        }
    }
}
