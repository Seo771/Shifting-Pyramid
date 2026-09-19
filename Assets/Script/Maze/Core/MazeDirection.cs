namespace ShiftingPyramid.Maze.Core
{
    /// <summary>
    /// 미로 타일 사이의 네 방향.
    /// 벽 연결 상태를 표시할 때 이 enum을 기준으로 사용한다.
    /// </summary>
    public enum MazeDirection
    {
        North,
        East,
        South,
        West
    }

    public static class MazeDirectionExtensions
    {
        // 한쪽 벽을 열면 이웃 타일의 반대쪽 벽도 같이 열어야 한다.
        public static MazeDirection Opposite(this MazeDirection direction)
        {
            switch (direction)
            {
                case MazeDirection.North:
                    return MazeDirection.South;
                case MazeDirection.East:
                    return MazeDirection.West;
                case MazeDirection.South:
                    return MazeDirection.North;
                case MazeDirection.West:
                    return MazeDirection.East;
                default:
                    return direction;
            }
        }

        // 방향을 격자 좌표 이동값으로 바꾼다.
        public static MazeCoordinate ToOffset(this MazeDirection direction)
        {
            switch (direction)
            {
                case MazeDirection.North:
                    return new MazeCoordinate(0, 1);
                case MazeDirection.East:
                    return new MazeCoordinate(1, 0);
                case MazeDirection.South:
                    return new MazeCoordinate(0, -1);
                case MazeDirection.West:
                    return new MazeCoordinate(-1, 0);
                default:
                    return MazeCoordinate.Zero;
            }
        }
    }
}
