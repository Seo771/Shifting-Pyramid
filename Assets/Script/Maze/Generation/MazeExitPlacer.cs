using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;

namespace ShiftingPyramid.Maze.Generation
{
    /// <summary>
    /// 시작점에서 통로를 따라 가장 먼 가장자리 칸을 출구로 지정한다.
    /// </summary>
    public class MazeExitPlacer
    {
        private static readonly MazeDirection[] Directions =
        {
            MazeDirection.North,
            MazeDirection.East,
            MazeDirection.South,
            MazeDirection.West
        };

        public MazeCoordinate Place(MazeGrid grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var start = MazeCoordinate.Zero;
            var visited = new HashSet<MazeCoordinate> { start };
            var queue = new Queue<(MazeCoordinate Coordinate, int Distance)>();
            queue.Enqueue((start, 0));

            MazeTile exit = null;
            var farthestDistance = -1;

            // 열린 통로만 따라가며 시작점에서의 실제 이동 거리를 계산한다.
            while (queue.Count > 0)
            {
                var (coordinate, distance) = queue.Dequeue();
                var tile = grid.GetTile(coordinate);

                if (!coordinate.Equals(start)
                    && tile.TileType == MazeTileType.Normal
                    && !tile.IsProtected
                    && IsEdge(grid, coordinate)
                    && distance > farthestDistance)
                {
                    exit = tile;
                    farthestDistance = distance;
                }

                foreach (var direction in Directions)
                {
                    if (!tile.IsOpen(direction)
                        || !grid.TryGetNeighbor(coordinate, direction, out var neighbor)
                        || !visited.Add(neighbor.Coordinate))
                    {
                        continue;
                    }

                    queue.Enqueue((neighbor.Coordinate, distance + 1));
                }
            }

            if (exit == null)
            {
                throw new InvalidOperationException("No reachable edge tile is available for the exit.");
            }

            exit.TileType = MazeTileType.Exit;
            exit.IsProtected = true;
            return exit.Coordinate;
        }

        private static bool IsEdge(MazeGrid grid, MazeCoordinate coordinate)
        {
            return coordinate.X == 0
                || coordinate.Y == 0
                || coordinate.X == grid.Width - 1
                || coordinate.Y == grid.Height - 1;
        }
    }
}
