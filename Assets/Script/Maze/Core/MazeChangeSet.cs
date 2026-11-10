using System;
using System.Collections.Generic;

namespace ShiftingPyramid.Maze.Core
{
    /// <summary>확정된 변경 좌표. 전체 재생성과 부분 변경을 구분한다.</summary>
    public sealed class MazeChangeSet
    {
        private MazeChangeSet(bool isFullRebuild, List<MazeCoordinate> cells)
        {
            IsFullRebuild = isFullRebuild;
            Cells = cells.AsReadOnly();
        }

        public bool IsFullRebuild { get; }
        public IReadOnlyList<MazeCoordinate> Cells { get; }

        public static MazeChangeSet Full(MazeGrid grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var cells = new List<MazeCoordinate>();
            foreach (var tile in grid.Tiles) cells.Add(tile.Coordinate);
            return new MazeChangeSet(true, cells);
        }

        public static MazeChangeSet Between(MazeGrid before, MazeGrid after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            if (before.Width != after.Width || before.Height != after.Height)
                throw new ArgumentException("Use Full for a newly sized maze.");
            var cells = new List<MazeCoordinate>();
            foreach (var tile in after.Tiles)
            {
                var previous = before.GetTile(tile.Coordinate);
                before.TryGetRoom(tile.Coordinate, out var oldRoom);
                after.TryGetRoom(tile.Coordinate, out var newRoom);
                var changed = tile.TileType != previous.TileType
                    || tile.IsProtected != previous.IsProtected || !ReferenceEquals(oldRoom, newRoom);
                for (var direction = MazeDirection.North; direction <= MazeDirection.West; direction++)
                    changed |= tile.IsOpen(direction) != previous.IsOpen(direction);
                if (changed) cells.Add(tile.Coordinate);
            }
            return new MazeChangeSet(false, cells);
        }
    }
}
