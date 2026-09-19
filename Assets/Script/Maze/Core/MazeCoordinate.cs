using System;

namespace ShiftingPyramid.Maze.Core
{
    /// <summary>
    /// 미로 안에서 사용하는 2D 격자 좌표.
    /// Unity 위치(Vector3)와 분리해서 미로 계산 로직을 독립적으로 유지한다.
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

        // 현재 좌표에 방향 이동값을 더할 때 사용한다.
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
