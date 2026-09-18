using System;

namespace ShiftingPyramid.Maze.Core
{
    /// <summary>
    /// 미로 안에서 사용하는 2D 격자 좌표.
    /// </summary>
    [Serializable]
    public readonly struct MazeCoordinate : IEquatable<MazeCoordinate>
    {
        public static readonly MazeCoordinate Zero = new MazeCoordinate(0, 0);

        public readonly int X;
        public readonly int Y;

        public MazeCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        // 좌표 이동 계산에 사용한다. 예: 현재 좌표 + North 방향 오프셋.
        public static MazeCoordinate operator +(MazeCoordinate left, MazeCoordinate right)
        {
            return new MazeCoordinate(left.X + right.X, left.Y + right.Y);
        }

        public static MazeCoordinate operator -(MazeCoordinate left, MazeCoordinate right)
        {
            return new MazeCoordinate(left.X - right.X, left.Y - right.Y);
        }

        public bool Equals(MazeCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is MazeCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
