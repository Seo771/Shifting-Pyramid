using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Generation;
using ShiftingPyramid.Maze.Settings;
using ShiftingPyramid.Maze.Validation;
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
        [SerializeField] private PlayerMazeSpawner playerSpawner;

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
                : new System.Random().Next();

            var generator = new DepthFirstMazeGenerator();
            currentGrid = generator.Generate(width, height, seed);

            var exitPlacer = new MazeExitPlacer();
            exitPlacer.Place(currentGrid);

            if (settings != null)
            {
                var roomPlacer = new MazeRoomPlacer();
                roomPlacer.Place(currentGrid, settings.TreasureRoomCount, settings.SpecialRoomCount, seed);
            }

            var requiredRooms = new List<MazeCoordinate>();
            foreach (var tile in currentGrid.Tiles)
            {
                if (tile.TileType == MazeTileType.Exit || tile.TileType == MazeTileType.TreasureRoom)
                {
                    requiredRooms.Add(tile.Coordinate);
                }
            }

            var pathValidator = new MazePathValidator();
            if (!pathValidator.AreReachable(currentGrid, MazeCoordinate.Zero, requiredRooms))
            {
                Debug.LogError("Maze generation failed: exit or treasure room is unreachable.", this);
                return;
            }

            mazeRenderer.Build(currentGrid, settings, seed);
            if (playerSpawner != null)
            {
                playerSpawner.SpawnAtStart(mazeRenderer);
            }
        }
    }
}
