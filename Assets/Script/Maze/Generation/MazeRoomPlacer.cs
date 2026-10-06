using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;

namespace ShiftingPyramid.Maze.Generation
{
    /// <summary>통로 생성 전에 3x3 방과 주변 한 칸의 통로 공간을 예약한다.</summary>
    public class MazeRoomPlacer
    {
        private const int PlacementAttempts = 128;

        // 실패 시 grid를 변경하지 않는다. 같은 seed는 같은 배치를 만든다.
        public void Place(MazeGrid grid, IReadOnlyList<MazeRoomTemplate> templates, int? seed = null)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (templates == null) throw new ArgumentNullException(nameof(templates));
            if (grid.Rooms.Count != 0) throw new ArgumentException("Rooms have already been reserved.");
            foreach (var tile in grid.Tiles)
                foreach (MazeDirection direction in Enum.GetValues(typeof(MazeDirection)))
                    if (tile.IsOpen(direction))
                        throw new ArgumentException("Place rooms before generating corridors.");
            foreach (var template in templates)
                if (template == null) throw new ArgumentException("Room template is null.");
            if (templates.Count == 0) return;

            var candidates = new List<MazeCoordinate>();
            // 바깥 테두리는 통로/출구용으로 남긴다.
            for (var x = 1; x + MazeRoom.Size < grid.Width; x++)
                for (var y = 1; y + MazeRoom.Size < grid.Height; y++)
                    candidates.Add(new MazeCoordinate(x, y));

            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            for (var attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                for (var i = candidates.Count - 1; i > 0; i--)
                {
                    var selected = random.Next(i + 1);
                    var swap = candidates[i];
                    candidates[i] = candidates[selected];
                    candidates[selected] = swap;
                }

                var planned = new List<MazeRoom>();
                foreach (var origin in candidates)
                {
                    var room = new MazeRoom(origin, templates[planned.Count], planned.Count);
                    if (!CanPlace(grid, room, planned)) continue;
                    planned.Add(room);
                    if (planned.Count != templates.Count) continue;
                    foreach (var placed in planned) grid.AddRoom(placed);
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Cannot place {templates.Count} 3x3 rooms in {grid.Width}x{grid.Height} with a one-cell corridor gap. "
                + "Increase Maze Width/Height or reduce room counts.");
        }

        private static bool CanPlace(MazeGrid grid, MazeRoom room, List<MazeRoom> planned)
        {
            foreach (var cell in room.Cells)
                if (cell.Equals(MazeCoordinate.Zero) || grid.GetTile(cell).IsProtected
                    || grid.GetTile(cell).TileType != MazeTileType.Normal) return false;

            foreach (var other in planned)
            {
                // 최소 한 칸을 띄워 방 주변의 일반 통로가 이어지도록 한다.
                var separated = room.Origin.X >= other.Origin.X + MazeRoom.Size + 1
                    || other.Origin.X >= room.Origin.X + MazeRoom.Size + 1
                    || room.Origin.Y >= other.Origin.Y + MazeRoom.Size + 1
                    || other.Origin.Y >= room.Origin.Y + MazeRoom.Size + 1;
                if (!separated) return false;
            }
            return true;
        }
    }
}
