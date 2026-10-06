using System.Collections.Generic;
using ShiftingPyramid.Maze.View;
using UnityEngine;

namespace ShiftingPyramid.Maze.Settings
{
    /// <summary>
    /// 미로 생성과 변경에 필요한 밸런싱 값을 모아두는 설정 에셋.
    /// 게임 승리/패배 규칙은 GameManager에서 관리한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MazeBalanceSettings", menuName = "Shifting Pyramid/Maze Balance Settings")]
    public class MazeBalanceSettings : ScriptableObject
    {
        [Header("Maze Size")]
        [SerializeField, Min(2)] private int mazeWidth = 15;
        [SerializeField, Min(2)] private int mazeHeight = 15;

        [Header("Rooms")]
        [SerializeField, Min(0)] private int treasureRoomCount = 5;
        [SerializeField, Min(0)] private int specialRoomCount = 2;

        [Header("3x3 Room Prefabs")]
        [SerializeField] private MazeRoomView treasureRoom3x3Prefab;
        [SerializeField] private List<MazeRoomView> specialRoom3x3Prefabs = new List<MazeRoomView>();
        // 기존 에셋의 1칸 프리팹 참조는 보존하되 새 생성에는 사용하지 않는다.
        [SerializeField, HideInInspector] private MazeTileView treasureRoomPrefab;
        [SerializeField, HideInInspector] private List<MazeTileView> specialRoomPrefabs = new List<MazeTileView>();

        [Header("Exit (1x1)")]
        [SerializeField] private MazeTileView exitRoomPrefab;

        [Header("Runtime Maze Change")]
        [SerializeField, Min(1)] private int mazeChangeRegionSize = 3;
        [SerializeField, Min(1f)] private float mazeChangeInterval = 20f;
        [SerializeField, Min(0)] private int playerSafeRadius = 1;
        [SerializeField, Min(0)] private int mummySafeRadius = 1;

        [Header("Random")]
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int seed = 12345;

        public int MazeWidth => mazeWidth;
        public int MazeHeight => mazeHeight;
        public int TreasureRoomCount => treasureRoomCount;
        public int SpecialRoomCount => specialRoomCount;
        public MazeRoomView TreasureRoom3x3Prefab => treasureRoom3x3Prefab;
        public IReadOnlyList<MazeRoomView> SpecialRoom3x3Prefabs => specialRoom3x3Prefabs;
        public MazeTileView ExitRoomPrefab => exitRoomPrefab;
        public int MazeChangeRegionSize => mazeChangeRegionSize;
        public float MazeChangeInterval => mazeChangeInterval;
        public int PlayerSafeRadius => playerSafeRadius;
        public int MummySafeRadius => mummySafeRadius;
        public bool UseRandomSeed => useRandomSeed;
        public int Seed => seed;

        private void OnValidate()
        {
            // 3x3, 5x5처럼 중심이 있는 구역을 쓰기 위해 홀수 크기로 맞춘다.
            if (mazeChangeRegionSize % 2 == 0)
            {
                mazeChangeRegionSize += 1;
            }
        }
    }
}
