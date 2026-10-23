using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>한 번 생성된 미로의 탐험 여부와 마지막 구조를 관리한다.</summary>
    public sealed class MapExplorationState
    {
        private readonly bool[,] explored;
        private readonly MapTileSignature[,] signatures;

        public MapExplorationState(MazeGrid grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            Width = grid.Width;
            Height = grid.Height;
            explored = new bool[Width, Height];
            signatures = new MapTileSignature[Width, Height];
            foreach (var tile in grid.Tiles)
                signatures[tile.Coordinate.X, tile.Coordinate.Y] =
                    MapTileSignature.Capture(grid, tile.Coordinate);
        }

        public int Width { get; }
        public int Height { get; }

        public bool IsExplored(MazeCoordinate coordinate)
        {
            return Contains(coordinate) && explored[coordinate.X, coordinate.Y];
        }

        public bool Reveal(MazeCoordinate coordinate)
        {
            if (!Contains(coordinate) || explored[coordinate.X, coordinate.Y]) return false;
            explored[coordinate.X, coordinate.Y] = true;
            return true;
        }

        // 미탐험 칸의 구조는 UI에 전달하지 않는다.
        public bool TryGetKnownTile(MazeCoordinate coordinate, out MapTileSignature signature)
        {
            if (IsExplored(coordinate))
            {
                signature = signatures[coordinate.X, coordinate.Y];
                return true;
            }

            signature = default;
            return false;
        }

        public IReadOnlyList<MazeCoordinate> Reconcile(MazeGrid grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (grid.Width != Width || grid.Height != Height)
                throw new ArgumentException("A new maze needs a new exploration state.", nameof(grid));

            var changed = new List<MazeCoordinate>();
            foreach (var tile in grid.Tiles)
            {
                var coordinate = tile.Coordinate;
                var next = MapTileSignature.Capture(grid, coordinate);
                if (signatures[coordinate.X, coordinate.Y].Equals(next)) continue;

                signatures[coordinate.X, coordinate.Y] = next;
                explored[coordinate.X, coordinate.Y] = false;
                changed.Add(coordinate);
            }

            return changed.AsReadOnly();
        }

        private bool Contains(MazeCoordinate coordinate)
        {
            return coordinate.X >= 0 && coordinate.Y >= 0
                && coordinate.X < Width && coordinate.Y < Height;
        }
    }
}
