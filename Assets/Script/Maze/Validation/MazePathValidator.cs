using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;

namespace ShiftingPyramid.Maze.Validation
{
    /// <summary>
    /// 열린 통로만 따라가며 미로 안의 목적지까지 이동 가능한지 검사한다.
    /// </summary>
    public class MazePathValidator
    {
        private static readonly MazeDirection[] Directions =
        {
            MazeDirection.North,
            MazeDirection.East,
            MazeDirection.South,
            MazeDirection.West
        };

        public bool IsReachable(MazeGrid grid, MazeCoordinate start, MazeCoordinate target)
        {
            return AreReachable(grid, start, new[] { target });
        }

        public bool AreReachable(MazeGrid grid, MazeCoordinate start, IEnumerable<MazeCoordinate> targets)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            if (!grid.Contains(start))
            {
                return false;
            }

            var remaining = new HashSet<MazeCoordinate>();
            foreach (var target in targets)
            {
                if (!grid.Contains(target))
                {
                    return false;
                }

                remaining.Add(target);
            }

            remaining.Remove(start);
            if (remaining.Count == 0)
            {
                return true;
            }

            var visited = new HashSet<MazeCoordinate> { start };
            var queue = new Queue<MazeCoordinate>();
            queue.Enqueue(start);

            // 열린 벽이 양쪽에서 일치하는 이웃 타일만 방문한다.
            while (queue.Count > 0)
            {
                var coordinate = queue.Dequeue();
                var tile = grid.GetTile(coordinate);

                foreach (var direction in Directions)
                {
                    if (!tile.IsOpen(direction)
                        || !grid.TryGetNeighbor(coordinate, direction, out var neighbor)
                        || !neighbor.IsOpen(direction.Opposite())
                        || !visited.Add(neighbor.Coordinate))
                    {
                        continue;
                    }

                    if (remaining.Remove(neighbor.Coordinate) && remaining.Count == 0)
                    {
                        return true;
                    }

                    queue.Enqueue(neighbor.Coordinate);
                }
            }

            return false;
        }
    }
}
