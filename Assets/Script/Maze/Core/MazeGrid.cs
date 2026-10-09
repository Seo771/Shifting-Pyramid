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
        private readonly List<MazeRoom> rooms = new List<MazeRoom>();
        private readonly Dictionary<MazeCoordinate, MazeRoom> roomCells = new Dictionary<MazeCoordinate, MazeRoom>();

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
        public IReadOnlyList<MazeRoom> Rooms => rooms.AsReadOnly();

        public bool TryGetRoom(MazeCoordinate coordinate, out MazeRoom room)
        {
            return roomCells.TryGetValue(coordinate, out room);
        }

        public void AddRoom(MazeRoom room)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            foreach (var cell in room.Cells)
            {
                if (!Contains(cell) || cell.Equals(MazeCoordinate.Zero)
                    || roomCells.ContainsKey(cell) || GetTile(cell).TileType != MazeTileType.Normal
                    || GetTile(cell).IsProtected)
                    throw new ArgumentException("Room footprint overlaps occupied/protected cells or the maze boundary.");
                foreach (MazeDirection direction in Enum.GetValues(typeof(MazeDirection)))
                    if (GetTile(cell).IsOpen(direction))
                        throw new ArgumentException("Reserve rooms before carving maze connections.");
            }
            foreach (var entrance in room.Entrances)
            {
                var outside = room.Origin + entrance.LocalCell + entrance.Direction.ToOffset();
                if (!Contains(outside) || roomCells.ContainsKey(outside))
                    throw new ArgumentException("Every entrance needs an ordinary corridor cell outside the room.");
            }
            foreach (var existing in rooms)
                foreach (var entrance in existing.Entrances)
                    if (room.Contains(existing.Origin + entrance.LocalCell + entrance.Direction.ToOffset()))
                        throw new ArgumentException("Room footprint would block an existing room entrance.");

            rooms.Add(room);
            foreach (var cell in room.Cells)
            {
                roomCells.Add(cell, room);
                var tile = GetTile(cell);
                tile.TileType = room.Template.TileType;
                // 특수방도 초기 배치 후 고정한다. 동적 교체는 별도 기능으로 구현한다.
                tile.IsProtected = true;
            }
            foreach (var cell in room.Cells)
            {
                if (room.Contains(cell + MazeDirection.North.ToOffset()))
                    SetConnection(cell, MazeDirection.North, true);
                if (room.Contains(cell + MazeDirection.East.ToOffset()))
                    SetConnection(cell, MazeDirection.East, true);
            }
        }

        // 방 내부 또는 표시된 출입구에서만 연결을 허용한다.
        public bool CanConnect(MazeCoordinate coordinate, MazeDirection direction)
        {
            var neighbor = coordinate + direction.ToOffset();
            if (!Contains(coordinate) || !Contains(neighbor)) return false;
            TryGetRoom(coordinate, out var room);
            TryGetRoom(neighbor, out var neighborRoom);
            if (room != null && ReferenceEquals(room, neighborRoom)) return true;
            return (room == null || room.HasEntrance(coordinate, direction))
                && (neighborRoom == null || neighborRoom.HasEntrance(neighbor, direction.Opposite()));
        }

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
            if (isOpen && (roomCells.ContainsKey(coordinate)
                || roomCells.ContainsKey(coordinate + direction.ToOffset())) && !CanConnect(coordinate, direction))
                throw new InvalidOperationException("Cannot carve through an undeclared room wall.");
            var tile = GetTile(coordinate);
            tile.SetOpen(direction, isOpen);

            if (TryGetNeighbor(coordinate, direction, out var neighbor))
            {
                neighbor.SetOpen(direction.Opposite(), isOpen);
            }
        }
    }
}
