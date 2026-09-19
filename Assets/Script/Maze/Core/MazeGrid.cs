using System;
using System.Collections.Generic;

namespace ShiftingPyramid.Maze.Core
{
    /// <summary>
    /// 미로 전체 타일을 좌표 기준으로 관리한다.
    /// 타일 사이 연결을 바꿀 때 양쪽 벽 상태를 함께 맞춘다.
    /// </summary>
    public class MazeGrid
    {
        private readonly MazeTile[,] tiles;

        public MazeGrid(int width, int height)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Maze width must be at least 1.");
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Maze height must be at least 1.");
            }

            Width = width;
            Height = height;
            tiles = new MazeTile[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    tiles[x, y] = new MazeTile(new MazeCoordinate(x, y));
                }
            }
        }

        public int Width { get; }
        public int Height { get; }

        public IEnumerable<MazeTile> Tiles
        {
            get
            {
                for (var x = 0; x < Width; x++)
                {
                    for (var y = 0; y < Height; y++)
                    {
                        yield return tiles[x, y];
                    }
                }
            }
        }

        public bool Contains(MazeCoordinate coordinate)
        {
            return coordinate.X >= 0
                && coordinate.Y >= 0
                && coordinate.X < Width
                && coordinate.Y < Height;
        }

        public MazeTile GetTile(MazeCoordinate coordinate)
        {
            if (!Contains(coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate), $"Coordinate {coordinate} is outside the maze.");
            }

            return tiles[coordinate.X, coordinate.Y];
        }

        public bool TryGetTile(MazeCoordinate coordinate, out MazeTile tile)
        {
            if (!Contains(coordinate))
            {
                tile = null;
                return false;
            }

            tile = tiles[coordinate.X, coordinate.Y];
            return true;
        }

        public bool TryGetNeighbor(MazeCoordinate coordinate, MazeDirection direction, out MazeTile neighbor)
        {
            return TryGetTile(coordinate + direction.ToOffset(), out neighbor);
        }

        // 두 타일 사이의 통로를 열거나 닫는다. 이웃이 없으면 현재 타일의 바깥 벽만 바꾼다.
        public void SetConnection(MazeCoordinate coordinate, MazeDirection direction, bool isOpen)
        {
            var tile = GetTile(coordinate);
            tile.SetOpen(direction, isOpen);

            if (TryGetNeighbor(coordinate, direction, out var neighbor))
            {
                neighbor.SetOpen(direction.Opposite(), isOpen);
            }
        }
    }
}
