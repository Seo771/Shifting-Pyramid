using UnityEngine;

namespace ShiftingPyramid.Maze.Settings
{
    /// <summary>
    /// 미로와 게임 진행에 필요한 밸런싱 값을 모아두는 설정 에셋.
    /// 코드 수정 없이 인스펙터에서 난이도와 생성 조건을 조정하기 위해 사용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MazeBalanceSettings", menuName = "Shifting Pyramid/Maze Balance Settings")]
    public class MazeBalanceSettings : ScriptableObject
    {
        [Header("Maze Size")]
        [SerializeField, Min(2)] private int mazeWidth = 10;
        [SerializeField, Min(2)] private int mazeHeight = 10;

        [Header("Rooms")]
        [SerializeField, Min(0)] private int treasureRoomCount = 5;
        [SerializeField, Min(0)] private int fixedSpecialRoomCount = 2;

        [Header("Game Rules")]
        [SerializeField, Min(0)] private int requiredTreasureCount = 3;

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
        public int FixedSpecialRoomCount => fixedSpecialRoomCount;
        public int RequiredTreasureCount => requiredTreasureCount;
        public int MazeChangeRegionSize => mazeChangeRegionSize;
        public float MazeChangeInterval => mazeChangeInterval;
        public int PlayerSafeRadius => playerSafeRadius;
        public int MummySafeRadius => mummySafeRadius;
        public bool UseRandomSeed => useRandomSeed;
        public int Seed => seed;

        private void OnValidate()
        {
            // 요구 보물 수가 실제 보물방 수보다 많으면 클리어가 불가능해질 수 있다.
            if (requiredTreasureCount > treasureRoomCount)
            {
                requiredTreasureCount = treasureRoomCount;
            }

            // 3x3, 5x5처럼 중심이 있는 구역을 쓰기 위해 홀수 크기로 맞춘다.
            if (mazeChangeRegionSize % 2 == 0)
            {
                mazeChangeRegionSize += 1;
            }
        }
    }
}
