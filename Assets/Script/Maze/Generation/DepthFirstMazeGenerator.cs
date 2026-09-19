using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;

namespace ShiftingPyramid.Maze.Generation
{
    /// <summary>
    /// 랜덤 DFS 방식으로 모든 칸이 연결된 기본 미로를 생성한다.
    /// Unity 오브젝트를 직접 다루지 않고 MazeGrid 데이터만 수정한다.
    /// </summary>
    public class DepthFirstMazeGenerator
    {
        private static readonly MazeDirection[] Directions =
        {
            MazeDirection.North,
            MazeDirection.East,
            MazeDirection.South,
            MazeDirection.West
        };

        public MazeGrid Generate(int width, int height, int? seed = null)
        {
            var grid = new MazeGrid(width, height);
            var random = seed.HasValue ? new Random(seed.Value) : new Random();

            GenerateInto(grid, random);
            return grid;
        }

        private void GenerateInto(MazeGrid grid, Random random)
        {
            var visited = new HashSet<MazeCoordinate>();
            var stack = new Stack<MazeCoordinate>();
            var start = MazeCoordinate.Zero;

            visited.Add(start);
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var candidates = GetUnvisitedNeighbors(grid, current, visited);

                if (candidates.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                var next = candidates[random.Next(candidates.Count)];

                grid.SetConnection(current, next.Direction, true);
                visited.Add(next.Coordinate);
                stack.Push(next.Coordinate);
            }
        }

        private static List<(MazeCoordinate Coordinate, MazeDirection Direction)> GetUnvisitedNeighbors(
            MazeGrid grid,
            MazeCoordinate current,
            HashSet<MazeCoordinate> visited)
        {
            var result = new List<(MazeCoordinate Coordinate, MazeDirection Direction)>();

            foreach (var direction in Directions)
            {
                var next = current + direction.ToOffset();

                if (grid.Contains(next) && !visited.Contains(next))
                {
                    result.Add((next, direction));
                }
            }

            return result;
        }
    }
}
