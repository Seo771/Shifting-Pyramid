using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.RuntimeChange;
using UnityEngine;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>게임 중 특수방과 일반 통로를 주기적으로 변경한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MazeTestBootstrapper))]
    public class MazeRuntimeChangeController : MonoBehaviour
    {
        [SerializeField] private Transform mummy;

        private readonly MazeRegionChanger changer = new MazeRegionChanger();
        private readonly SpecialRoomReplacer roomReplacer = new SpecialRoomReplacer();
        private MazeTestBootstrapper bootstrapper;
        private MazeGrid activeGrid;
        private float timeUntilChange;
        private int changeCount;

        public int SecondsRemaining => activeGrid == null
            ? -1 : Mathf.CeilToInt(Mathf.Max(0f, timeUntilChange));

        private void Awake()
        {
            bootstrapper = GetComponent<MazeTestBootstrapper>();
        }

        private void Update()
        {
            var grid = bootstrapper.CurrentGrid;
            var settings = bootstrapper.Settings;
            var renderer = bootstrapper.Renderer;
            var player = bootstrapper.PlayerTransform;
            if (grid == null || settings == null || renderer == null || player == null) return;

            if (!ReferenceEquals(activeGrid, grid))
            {
                activeGrid = grid;
                timeUntilChange = settings.MazeChangeInterval;
                changeCount = 0;
            }

            if (GameManager.Instance != null
                && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
            if (!renderer.TryGetCoordinate(player.position, grid, out var playerCoordinate)) return;

            MazeCoordinate? mummyCoordinate = null;
            if (mummy != null)
            {
                if (!renderer.TryGetCoordinate(mummy.position, grid, out var coordinate)) return;
                mummyCoordinate = coordinate;
            }

            timeUntilChange -= Time.deltaTime;
            if (timeUntilChange > 0f) return;
            timeUntilChange = settings.MazeChangeInterval;

            // 같은 미로 시드에서는 변경 순서도 재현할 수 있게 시드를 나눠 쓴다.
            var seed = unchecked(bootstrapper.CurrentSeed + ++changeCount);
            MazeRoom removedRoom = null;
            MazeRoom addedRoom = null;
            var roomChanged = bootstrapper.CurrentRoomTemplates != null
                && roomReplacer.TryReplace(grid, bootstrapper.CurrentRoomTemplates,
                    playerCoordinate, settings.PlayerSafeRadius, mummyCoordinate,
                    settings.MummySafeRadius, out removedRoom, out addedRoom, seed);
            var regionsChanged = changer.ChangeRegions(grid, settings.MazeChangeRegionSize,
                settings.MazeChangeRegionCount, playerCoordinate, settings.PlayerSafeRadius,
                mummyCoordinate, settings.MummySafeRadius, unchecked(seed + 1)).Count > 0;
            if (roomChanged)
                renderer.ApplyRoomReplacement(grid, removedRoom, addedRoom, bootstrapper.CurrentRoomPrefabs);
            if (roomChanged || regionsChanged)
                renderer.Refresh(grid);
        }
    }
}
