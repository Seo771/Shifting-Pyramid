using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.RuntimeChange;
using UnityEngine;
using System.Collections.Generic;

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
        public int LastChangedRegionCount { get; private set; }

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
            if (!bootstrapper.IsMazeReady || grid == null || settings == null || renderer == null || player == null) return;

            if (!ReferenceEquals(activeGrid, grid))
            {
                activeGrid = grid;
                timeUntilChange = settings.MazeChangeInterval;
                changeCount = 0;
            }

            if (GameManager.Instance != null
                && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
            if (!renderer.TryGetCoordinate(player.position, grid, out var playerCoordinate)) return;

            timeUntilChange -= Time.deltaTime;
            if (timeUntilChange > 0f) return;
            timeUntilChange = settings.MazeChangeInterval;

            // 모든 활성 미라를 보호한다. 기존 Inspector 참조도 계속 지원한다.
            var protectedAreas = new List<MazeProtectedArea>();
            foreach (var monster in FindObjectsByType<MonsterAI>(FindObjectsSortMode.None))
                if (monster.isActiveAndEnabled && !AddProtectedArea(monster.transform, grid, renderer,
                    settings.MummySafeRadius, protectedAreas)) return;
            if (mummy != null && !AddProtectedArea(mummy, grid, renderer,
                settings.MummySafeRadius, protectedAreas)) return;

            // 같은 미로 시드에서는 변경 순서도 재현할 수 있게 시드를 나눠 쓴다.
            var seed = unchecked(bootstrapper.CurrentSeed + ++changeCount);
            // 계산 중 예외가 나도 플레이 중인 격자는 바뀌지 않도록 계획을 분리한다.
            var proposed = grid.Clone();
            MazeRoom removedRoom = null;
            MazeRoom addedRoom = null;
            var roomChanged = bootstrapper.CurrentRoomTemplates != null
                && roomReplacer.TryReplace(proposed, bootstrapper.CurrentRoomTemplates,
                    playerCoordinate, settings.PlayerSafeRadius, null,
                    settings.MummySafeRadius, out removedRoom, out addedRoom, seed, protectedAreas);
            var regions = changer.ChangeRegions(proposed, settings.MazeChangeRegionSize,
                settings.MazeChangeRegionCount, playerCoordinate, settings.PlayerSafeRadius,
                null, settings.MummySafeRadius, unchecked(seed + 1), protectedAreas);
            LastChangedRegionCount = regions.Count;
            if (roomChanged || regions.Count > 0)
                bootstrapper.ApplyRuntimeChange(proposed, MazeChangeSet.Between(grid, proposed), removedRoom, addedRoom);
        }

        private bool AddProtectedArea(Transform character, MazeGrid grid, MazeRenderer renderer,
            int radius, List<MazeProtectedArea> areas)
        {
            if (!renderer.TryGetCoordinate(character.position, grid, out var cell))
            {
                Debug.LogWarning($"Maze change skipped: '{character.name}' is outside the maze.", this);
                return false;
            }
            areas.Add(new MazeProtectedArea(cell, radius));
            return true;
        }
    }
}
