using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Validation;

namespace ShiftingPyramid.Maze.RuntimeChange
{
    /// <summary>보호된 칸을 유지하면서 한 구역의 일반 통로를 다시 만든다.</summary>
    public class MazeRegionChanger
    {
        private static readonly MazeDirection[] Directions =
        {
            MazeDirection.North, MazeDirection.East, MazeDirection.South, MazeDirection.West
        };

        public bool TryChange(MazeGrid grid, int regionSize, MazeCoordinate playerCoordinate,
            int playerSafeRadius, MazeCoordinate? mummyCoordinate, int mummySafeRadius,
            out MazeCoordinate changedOrigin, int? seed = null)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (regionSize < 1 || regionSize % 2 == 0)
                throw new ArgumentOutOfRangeException(nameof(regionSize), "Region size must be a positive odd number.");
            if (playerSafeRadius < 0) throw new ArgumentOutOfRangeException(nameof(playerSafeRadius));
            if (mummySafeRadius < 0) throw new ArgumentOutOfRangeException(nameof(mummySafeRadius));
            if (!grid.Contains(playerCoordinate)) throw new ArgumentOutOfRangeException(nameof(playerCoordinate));
            if (mummyCoordinate.HasValue && !grid.Contains(mummyCoordinate.Value))
                throw new ArgumentOutOfRangeException(nameof(mummyCoordinate));

            changedOrigin = default;
            if (regionSize > grid.Width || regionSize > grid.Height) return false;

            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            var origins = new List<MazeCoordinate>();
            var targets = new List<MazeCoordinate>();
            foreach (var tile in grid.Tiles) targets.Add(tile.Coordinate);
            for (var x = 0; x <= grid.Width - regionSize; x++)
                for (var y = 0; y <= grid.Height - regionSize; y++)
                    origins.Add(new MazeCoordinate(x, y));

            for (var i = origins.Count - 1; i > 0; i--)
            {
                var selected = random.Next(i + 1);
                var swap = origins[i];
                origins[i] = origins[selected];
                origins[selected] = swap;
            }

            var validator = new MazePathValidator();
            foreach (var origin in origins)
            {
                var mutable = GetMutableCells(grid, origin, regionSize, playerCoordinate,
                    playerSafeRadius, mummyCoordinate, mummySafeRadius);
                var edges = GetInternalEdges(grid, mutable);
                if (edges.Count == 0) continue;

                foreach (var edge in edges)
                    grid.SetConnection(edge.Cell, edge.Direction, false);
                CarveComponents(grid, mutable, random);

                if (HasChanged(grid, edges) && validator.AreReachable(grid, playerCoordinate, targets))
                {
                    changedOrigin = origin;
                    return true;
                }

                // 도달성이 깨지거나 벽 모양이 같으면 원래 연결을 모두 복구한다.
                foreach (var edge in edges)
                    grid.SetConnection(edge.Cell, edge.Direction, edge.WasOpen);
            }

            return false;
        }

        private static List<MazeCoordinate> GetMutableCells(MazeGrid grid, MazeCoordinate origin,
            int size, MazeCoordinate player, int playerRadius, MazeCoordinate? mummy, int mummyRadius)
        {
            var cells = new List<MazeCoordinate>();
            for (var x = origin.X; x < origin.X + size; x++)
                for (var y = origin.Y; y < origin.Y + size; y++)
                {
                    var coordinate = new MazeCoordinate(x, y);
                    var tile = grid.GetTile(coordinate);
                    if (tile.IsProtected || tile.TileType != MazeTileType.Normal
                        || IsInSafeArea(coordinate, player, playerRadius)
                        || mummy.HasValue && IsInSafeArea(coordinate, mummy.Value, mummyRadius))
                        continue;
                    cells.Add(coordinate);
                }
            return cells;
        }

        private static bool IsInSafeArea(MazeCoordinate cell, MazeCoordinate center, int radius)
        {
            return Math.Abs(cell.X - center.X) <= radius && Math.Abs(cell.Y - center.Y) <= radius;
        }

        private static List<(MazeCoordinate Cell, MazeDirection Direction, bool WasOpen)> GetInternalEdges(
            MazeGrid grid, List<MazeCoordinate> cells)
        {
            var set = new HashSet<MazeCoordinate>(cells);
            var edges = new List<(MazeCoordinate Cell, MazeDirection Direction, bool WasOpen)>();
            foreach (var cell in cells)
                foreach (var direction in new[] { MazeDirection.North, MazeDirection.East })
                    if (set.Contains(cell + direction.ToOffset()))
                        edges.Add((cell, direction, grid.GetTile(cell).IsOpen(direction)));
            return edges;
        }

        private static void CarveComponents(MazeGrid grid, List<MazeCoordinate> cells, Random random)
        {
            var set = new HashSet<MazeCoordinate>(cells);
            var visited = new HashSet<MazeCoordinate>();
            var stack = new Stack<MazeCoordinate>();
            foreach (var start in cells)
            {
                if (!visited.Add(start)) continue;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var current = stack.Peek();
                    var options = new List<MazeDirection>();
                    foreach (var direction in Directions)
                        if (set.Contains(current + direction.ToOffset())
                            && !visited.Contains(current + direction.ToOffset()))
                            options.Add(direction);

                    if (options.Count == 0)
                    {
                        stack.Pop();
                        continue;
                    }

                    var nextDirection = options[random.Next(options.Count)];
                    grid.SetConnection(current, nextDirection, true);
                    var next = current + nextDirection.ToOffset();
                    visited.Add(next);
                    stack.Push(next);
                }
            }
        }

        private static bool HasChanged(MazeGrid grid,
            List<(MazeCoordinate Cell, MazeDirection Direction, bool WasOpen)> edges)
        {
            foreach (var edge in edges)
                if (grid.GetTile(edge.Cell).IsOpen(edge.Direction) != edge.WasOpen)
                    return true;
            return false;
        }
    }
}
