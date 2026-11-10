using System;
using System.Collections.Generic;

namespace ShiftingPyramid.Maze.Core
{
    /// <summary>캐릭터 오브젝트 대신 계산 계층에 전달하는 보호 영역.</summary>
    public readonly struct MazeProtectedArea
    {
        public MazeProtectedArea(MazeCoordinate center, int radius)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            Center = center;
            Radius = radius;
        }

        public MazeCoordinate Center { get; }
        public int Radius { get; }

        public static bool Contains(IReadOnlyList<MazeProtectedArea> areas,
            MazeCoordinate cell, int padding = 0)
        {
            if (areas == null) return false;
            foreach (var area in areas)
                if (Math.Abs(cell.X - area.Center.X) <= area.Radius + padding
                    && Math.Abs(cell.Y - area.Center.Y) <= area.Radius + padding) return true;
            return false;
        }
    }
}
