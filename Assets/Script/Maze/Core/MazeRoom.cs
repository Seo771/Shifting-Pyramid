using System;
using System.Collections.Generic;

namespace ShiftingPyramid.Maze.Core
{
    /// <summary>방 남서쪽을 (0,0)으로 하는 경계 칸과 바깥 방향.</summary>
    public readonly struct MazeRoomEntrance
    {
        public MazeRoomEntrance(MazeCoordinate localCell, MazeDirection direction)
        {
            if (localCell.X < 0 || localCell.Y < 0
                || localCell.X >= MazeRoom.Size || localCell.Y >= MazeRoom.Size)
                throw new ArgumentException("Room entrance cell must be inside the 3x3 footprint.");

            var outward = direction == MazeDirection.North && localCell.Y == 2
                || direction == MazeDirection.East && localCell.X == 2
                || direction == MazeDirection.South && localCell.Y == 0
                || direction == MazeDirection.West && localCell.X == 0;
            if (!outward)
                throw new ArgumentException("Room entrance must point out of its boundary cell.");

            LocalCell = localCell;
            Direction = direction;
        }

        public MazeCoordinate LocalCell { get; }
        public MazeDirection Direction { get; }
    }

    /// <summary>Unity 프리팹 참조 없이 생성기에 전달하는 방 구조.</summary>
    public sealed class MazeRoomTemplate
    {
        public MazeRoomTemplate(MazeTileType tileType, IEnumerable<MazeRoomEntrance> entrances)
        {
            if (tileType != MazeTileType.TreasureRoom && tileType != MazeTileType.SpecialRoom)
                throw new ArgumentException("Only treasure and special rooms use 3x3 footprints.");
            if (entrances == null) throw new ArgumentNullException(nameof(entrances));

            var copy = new List<MazeRoomEntrance>(entrances);
            if (copy.Count == 0) throw new ArgumentException("A room needs at least one entrance.");
            var unique = new HashSet<(MazeCoordinate, MazeDirection)>();
            foreach (var entrance in copy)
            {
                _ = new MazeRoomEntrance(entrance.LocalCell, entrance.Direction);
                if (!unique.Add((entrance.LocalCell, entrance.Direction)))
                    throw new ArgumentException("Duplicate room entrance.");
            }

            TileType = tileType;
            Entrances = copy.AsReadOnly();
        }

        public MazeTileType TileType { get; }
        public IReadOnlyList<MazeRoomEntrance> Entrances { get; }
    }

    /// <summary>3x3 점유 영역. 모든 내부 칸은 서로 이동 가능하다고 가정한다.</summary>
    public sealed class MazeRoom
    {
        public const int Size = 3;
        private readonly IReadOnlyList<MazeRoomEntrance> entrances;

        public MazeRoom(MazeCoordinate origin, MazeRoomTemplate template, int templateIndex,
            int rotationQuarterTurns = 0)
        {
            if (rotationQuarterTurns < 0 || rotationQuarterTurns > 3)
                throw new ArgumentOutOfRangeException(nameof(rotationQuarterTurns));
            Origin = origin;
            Template = template ?? throw new ArgumentNullException(nameof(template));
            TemplateIndex = templateIndex;
            RotationQuarterTurns = rotationQuarterTurns;

            // 원본 프리팹의 표시점은 유지하고, 배치된 방의 문 좌표만 회전한다.
            var rotated = new List<MazeRoomEntrance>();
            foreach (var entrance in template.Entrances)
            {
                var cell = entrance.LocalCell;
                var direction = entrance.Direction;
                for (var i = 0; i < rotationQuarterTurns; i++)
                {
                    cell = new MazeCoordinate(cell.Y, Size - 1 - cell.X);
                    direction = (MazeDirection)(((int)direction + 1) % 4);
                }
                rotated.Add(new MazeRoomEntrance(cell, direction));
            }
            entrances = rotated.AsReadOnly();
        }

        public MazeCoordinate Origin { get; }
        public MazeCoordinate Center => Origin + new MazeCoordinate(1, 1);
        public MazeRoomTemplate Template { get; }
        public int TemplateIndex { get; }
        public int RotationQuarterTurns { get; }
        public IReadOnlyList<MazeRoomEntrance> Entrances => entrances;

        public bool Contains(MazeCoordinate coordinate)
        {
            return coordinate.X >= Origin.X && coordinate.X < Origin.X + Size
                && coordinate.Y >= Origin.Y && coordinate.Y < Origin.Y + Size;
        }

        public IEnumerable<MazeCoordinate> Cells
        {
            get
            {
                for (var x = 0; x < Size; x++)
                    for (var y = 0; y < Size; y++)
                        yield return Origin + new MazeCoordinate(x, y);
            }
        }

        public bool HasEntrance(MazeCoordinate cell, MazeDirection direction)
        {
            foreach (var entrance in entrances)
                if ((Origin + entrance.LocalCell).Equals(cell) && entrance.Direction == direction)
                    return true;
            return false;
        }
    }
}
