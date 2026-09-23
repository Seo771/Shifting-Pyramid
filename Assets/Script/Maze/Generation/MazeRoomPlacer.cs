using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;

namespace ShiftingPyramid.Maze.Generation
{
    /// <summary>
    /// 생성된 미로에서 보물방과 특수방의 좌표를 겹치지 않게 정한다.
    /// </summary>
    public class MazeRoomPlacer
    {
        public void Place(MazeGrid grid, int treasureRoomCount, int specialRoomCount, int? seed = null)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (treasureRoomCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(treasureRoomCount));
            }

            if (specialRoomCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(specialRoomCount));
            }

            var candidates = new List<MazeTile>();
            foreach (var tile in grid.Tiles)
            {
                if (!tile.Coordinate.Equals(MazeCoordinate.Zero)
                    && tile.TileType == MazeTileType.Normal
                    && !tile.IsProtected)
                {
                    candidates.Add(tile);
                }
            }

            if ((long)treasureRoomCount + specialRoomCount > candidates.Count)
            {
                throw new ArgumentException(
                    $"Requested {treasureRoomCount} treasure rooms and {specialRoomCount} special rooms, "
                    + $"but only {candidates.Count} tiles are available.");
            }

            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            var roomCount = treasureRoomCount + specialRoomCount;

            // 필요한 칸만 섞어 선택하므로 같은 좌표가 두 번 뽑히지 않는다.
            for (var i = 0; i < roomCount; i++)
            {
                var selectedIndex = random.Next(i, candidates.Count);
                var selected = candidates[selectedIndex];
                candidates[selectedIndex] = candidates[i];
                candidates[i] = selected;

                if (i < treasureRoomCount)
                {
                    selected.TileType = MazeTileType.TreasureRoom;
                    selected.IsProtected = true;
                }
                else
                {
                    selected.TileType = MazeTileType.SpecialRoom;
                }
            }
        }
    }
}
