using System;
using ShiftingPyramid.Maze.Core;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>지도에서 비교할 칸의 구조를 값으로 보관한다.</summary>
    public readonly struct MapTileSignature : IEquatable<MapTileSignature>
    {
        private readonly byte openings;

        private MapTileSignature(MazeTileType tileType, byte openings, bool hasRoom,
            MazeCoordinate roomOrigin, int templateIndex, int rotationQuarterTurns)
        {
            TileType = tileType;
            this.openings = openings;
            HasRoom = hasRoom;
            RoomOrigin = roomOrigin;
            TemplateIndex = templateIndex;
            RotationQuarterTurns = rotationQuarterTurns;
        }

        public MazeTileType TileType { get; }
        public bool HasRoom { get; }
        public MazeCoordinate RoomOrigin { get; }
        public int TemplateIndex { get; }
        public int RotationQuarterTurns { get; }

        public bool IsOpen(MazeDirection direction)
        {
            if ((int)direction < 0 || (int)direction > 3)
                throw new ArgumentOutOfRangeException(nameof(direction));
            return (openings & (1 << (int)direction)) != 0;
        }

        public static MapTileSignature Capture(MazeGrid grid, MazeCoordinate coordinate)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var tile = grid.GetTile(coordinate);
            byte open = 0;
            for (var direction = MazeDirection.North; direction <= MazeDirection.West; direction++)
                if (tile.IsOpen(direction)) open |= (byte)(1 << (int)direction);

            if (grid.TryGetRoom(coordinate, out var room))
                return new MapTileSignature(tile.TileType, open, true, room.Origin,
                    room.TemplateIndex, room.RotationQuarterTurns);
            return new MapTileSignature(tile.TileType, open, false, default, -1, 0);
        }

        public bool Equals(MapTileSignature other)
        {
            return TileType == other.TileType && openings == other.openings
                && HasRoom == other.HasRoom && RoomOrigin.Equals(other.RoomOrigin)
                && TemplateIndex == other.TemplateIndex
                && RotationQuarterTurns == other.RotationQuarterTurns;
        }

        public override bool Equals(object obj)
        {
            return obj is MapTileSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)TileType;
                hash = hash * 397 ^ openings;
                hash = hash * 397 ^ HasRoom.GetHashCode();
                hash = hash * 397 ^ RoomOrigin.GetHashCode();
                hash = hash * 397 ^ TemplateIndex;
                return hash * 397 ^ RotationQuarterTurns;
            }
        }
    }
}
