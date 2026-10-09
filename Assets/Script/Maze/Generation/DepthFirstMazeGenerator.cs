using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;

namespace ShiftingPyramid.Maze.Generation
{
    /// <summary>일반 칸과 3x3 방을 각각 하나의 방문 단위로 삼는 랜덤 DFS.</summary>
    public class DepthFirstMazeGenerator
    {
        private static readonly MazeDirection[] Directions =
        {
            MazeDirection.North, MazeDirection.East, MazeDirection.South, MazeDirection.West
        };

        public MazeGrid Generate(int width, int height, int? seed = null)
        {
            var grid = new MazeGrid(width, height);
            GenerateInto(grid, seed);
            return grid;
        }

        public void GenerateInto(MazeGrid grid, int? seed = null)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            foreach (var tile in grid.Tiles)
                foreach (var direction in Directions)
                    if (tile.IsOpen(direction)
                        && (!grid.TryGetRoom(tile.Coordinate, out var room)
                            || !room.Contains(tile.Coordinate + direction.ToOffset())))
                        throw new ArgumentException("GenerateInto requires uncarved corridors.");

            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            var visited = new HashSet<MazeCoordinate> { MazeCoordinate.Zero };
            var stack = new Stack<MazeCoordinate>();
            stack.Push(MazeCoordinate.Zero);

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var candidates = new List<(MazeCoordinate Cell, MazeDirection Direction, MazeCoordinate Node)>();
                if (grid.TryGetRoom(current, out var room))
                {
                    foreach (var entrance in room.Entrances)
                        AddCandidate(grid, room.Origin + entrance.LocalCell, entrance.Direction, visited, candidates);
                }
                else
                {
                    foreach (var direction in Directions)
                        AddCandidate(grid, current, direction, visited, candidates);
                }

                if (candidates.Count == 0)
                {
                    stack.Pop();
                    continue;
                }
                var next = candidates[random.Next(candidates.Count)];
                grid.SetConnection(next.Cell, next.Direction, true);
                visited.Add(next.Node);
                stack.Push(next.Node);
            }

            // 제작자가 표시한 문은 모두 연결한다. 방을 통한 순환 경로를 허용한다.
            foreach (var room in grid.Rooms)
                foreach (var entrance in room.Entrances)
                    grid.SetConnection(room.Origin + entrance.LocalCell, entrance.Direction, true);

            foreach (var tile in grid.Tiles)
            {
                var node = grid.TryGetRoom(tile.Coordinate, out var room) ? room.Center : tile.Coordinate;
                if (!visited.Contains(node))
                    throw new InvalidOperationException($"Maze contains an unreachable unit at {node}.");
            }
        }

        private static void AddCandidate(MazeGrid grid, MazeCoordinate cell, MazeDirection direction,
            HashSet<MazeCoordinate> visited,
            List<(MazeCoordinate Cell, MazeDirection Direction, MazeCoordinate Node)> candidates)
        {
            if (!grid.CanConnect(cell, direction)) return;
            var neighbor = cell + direction.ToOffset();
            var node = grid.TryGetRoom(neighbor, out var room) ? room.Center : neighbor;
            if (!visited.Contains(node)) candidates.Add((cell, direction, node));
        }
    }
}
