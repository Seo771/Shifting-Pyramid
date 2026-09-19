using System;

namespace ShiftingPyramid.Maze.Core
{
    /// <summary>
    /// 미로 타일 하나의 논리 데이터.
    /// Unity 오브젝트가 아니라 좌표와 벽 상태만 관리한다.
    /// </summary>
    [Serializable]
    public class MazeTile
    {
        private readonly bool[] openDirections = new bool[4];

        public MazeTile(MazeCoordinate coordinate)
        {
            Coordinate = coordinate;
        }

        public MazeCoordinate Coordinate { get; }
        public MazeTileType TileType { get; set; } = MazeTileType.Normal;
        public bool IsProtected { get; set; }

        // true면 해당 방향으로 이동할 수 있다.
        public bool IsOpen(MazeDirection direction)
        {
            return openDirections[(int)direction];
        }

        // 벽이 있는지 확인할 때 사용한다. View 쪽에서 벽 오브젝트 On/Off에 쓰기 좋다.
        public bool HasWall(MazeDirection direction)
        {
            return !IsOpen(direction);
        }

        internal void SetOpen(MazeDirection direction, bool isOpen)
        {
            openDirections[(int)direction] = isOpen;
        }
    }
}
