using ShiftingPyramid.Maze.Core;
using UnityEngine;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>
    /// MazeTile 데이터를 Unity 프리팹의 벽 오브젝트 상태로 보여주는 스크립트.
    /// 미로 생성 로직은 넣지 않고, 벽 On/Off만 담당한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class MazeTileView : MonoBehaviour
    {
        [Header("Walls")]
        [SerializeField] private GameObject northWall;
        [SerializeField] private GameObject eastWall;
        [SerializeField] private GameObject southWall;
        [SerializeField] private GameObject westWall;

        public MazeCoordinate Coordinate { get; private set; }

        // Renderer가 프리팹을 생성한 뒤 이 타일의 좌표를 알려줄 때 사용한다.
        public void Initialize(MazeCoordinate coordinate)
        {
            Coordinate = coordinate;
        }

        // MazeTile의 벽 데이터를 읽어서 프리팹의 벽 오브젝트를 켜고 끈다.
        public void Apply(MazeTile tile)
        {
            Initialize(tile.Coordinate);

            SetWallActive(MazeDirection.North, tile.HasWall(MazeDirection.North));
            SetWallActive(MazeDirection.East, tile.HasWall(MazeDirection.East));
            SetWallActive(MazeDirection.South, tile.HasWall(MazeDirection.South));
            SetWallActive(MazeDirection.West, tile.HasWall(MazeDirection.West));
        }

        // 테스트나 디버깅 때 특정 방향 벽만 직접 바꾸고 싶을 때 사용한다.
        public void SetWallActive(MazeDirection direction, bool isActive)
        {
            var wall = GetWall(direction);

            if (wall != null)
            {
                wall.SetActive(isActive);
            }
        }

        private GameObject GetWall(MazeDirection direction)
        {
            switch (direction)
            {
                case MazeDirection.North:
                    return northWall;
                case MazeDirection.East:
                    return eastWall;
                case MazeDirection.South:
                    return southWall;
                case MazeDirection.West:
                    return westWall;
                default:
                    return null;
            }
        }

        private void Reset()
        {
            AutoAssignWalls();
        }

        private void OnValidate()
        {
            AutoAssignWalls();
        }

        // 프리팹 자식 이름이 Wall_North 같은 형식이면 자동으로 연결한다.
        private void AutoAssignWalls()
        {
            if (northWall == null)
            {
                northWall = FindChildByName("Wall_North");
            }

            if (eastWall == null)
            {
                eastWall = FindChildByName("Wall_East");
            }

            if (southWall == null)
            {
                southWall = FindChildByName("Wall_South");
            }

            if (westWall == null)
            {
                westWall = FindChildByName("Wall_West");
            }
        }

        private GameObject FindChildByName(string targetName)
        {
            foreach (var childTransform in GetComponentsInChildren<Transform>(true))
            {
                if (childTransform == transform)
                {
                    continue;
                }

                if (childTransform.name.Trim() == targetName)
                {
                    return childTransform.gameObject;
                }
            }

            return null;
        }
    }
}
