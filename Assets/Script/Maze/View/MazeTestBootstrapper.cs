using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Generation;
using ShiftingPyramid.Maze.Settings;
using UnityEngine;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>
    /// 테스트 씬에서 미로 타일 배치를 빠르게 확인하기 위한 임시 부트스트래퍼.
    /// 나중에 실제 미로 생성 흐름이 생기면 교체하거나 제거해도 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class MazeTestBootstrapper : MonoBehaviour
    {
        [SerializeField] private MazeRenderer mazeRenderer;
        [SerializeField] private MazeBalanceSettings settings;

        [Header("Fallback Size")]
        [SerializeField] private int fallbackWidth = 5;
        [SerializeField] private int fallbackHeight = 5;

        private MazeGrid currentGrid;

        public MazeGrid CurrentGrid => currentGrid;

        private void Start()
        {
            BuildTestMaze();
        }

        [ContextMenu("Build Test Maze")]
        public void BuildTestMaze()
        {
            if (mazeRenderer == null)
            {
                mazeRenderer = FindFirstObjectByType<MazeRenderer>();
            }

            if (mazeRenderer == null)
            {
                Debug.LogWarning("MazeTestBootstrapper failed: MazeRenderer is not assigned.", this);
                return;
            }

            var width = settings != null ? settings.MazeWidth : fallbackWidth;
            var height = settings != null ? settings.MazeHeight : fallbackHeight;

            var seed = settings != null && !settings.UseRandomSeed
                ? settings.Seed
                : (int?)null;

            var generator = new DepthFirstMazeGenerator();
            currentGrid = generator.Generate(width, height, seed);

            mazeRenderer.Build(currentGrid);
        }
    }
}
