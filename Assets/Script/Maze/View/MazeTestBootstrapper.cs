using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Generation;
using ShiftingPyramid.Maze.Settings;
using ShiftingPyramid.Maze.Validation;
using UnityEngine;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>방 예약 → DFS → 출구 → BFS 검증 → 프리팹 표시.</summary>
    [DisallowMultipleComponent]
    public class MazeTestBootstrapper : MonoBehaviour
    {
        [SerializeField] private MazeRenderer mazeRenderer;
        [SerializeField] private MazeBalanceSettings settings;
        [SerializeField] private PlayerMazeSpawner playerSpawner;

        [Header("Fallback Size (No Rooms)")]
        [SerializeField] private int fallbackWidth = 5;
        [SerializeField] private int fallbackHeight = 5;

        private MazeGrid currentGrid;
        public MazeGrid CurrentGrid => currentGrid;
        public int CurrentSeed { get; private set; }

        private void Start()
        {
            BuildTestMaze();
        }

        [ContextMenu("Build Test Maze")]
        public void BuildTestMaze()
        {
            if (mazeRenderer == null) mazeRenderer = FindFirstObjectByType<MazeRenderer>();
            if (mazeRenderer == null)
            {
                Debug.LogWarning("MazeTestBootstrapper: MazeRenderer is not assigned.", this);
                return;
            }

            var seed = settings != null && !settings.UseRandomSeed
                ? settings.Seed : new System.Random().Next();

            try
            {
                mazeRenderer.ValidateConfiguration();
                var width = settings != null ? settings.MazeWidth : fallbackWidth;
                var height = settings != null ? settings.MazeHeight : fallbackHeight;
                var grid = new MazeGrid(width, height);
                var templates = new List<MazeRoomTemplate>();
                var prefabs = SelectRoomPrefabs(templates, seed);

                new MazeRoomPlacer().Place(grid, templates, seed);
                new DepthFirstMazeGenerator().GenerateInto(grid, seed);
                var exit = new MazeExitPlacer().Place(grid);
                var targets = new List<MazeCoordinate> { exit };
                foreach (var room in grid.Rooms) targets.Add(room.Center);

                if (!new MazePathValidator().AreReachable(grid, MazeCoordinate.Zero, targets))
                    throw new InvalidOperationException("An exit or room is unreachable.");

                mazeRenderer.Build(grid, settings, seed, prefabs);
                currentGrid = grid;
                CurrentSeed = seed;
                if (playerSpawner == null) playerSpawner = FindFirstObjectByType<PlayerMazeSpawner>();
                if (playerSpawner != null) playerSpawner.SpawnAtStart(mazeRenderer);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                Debug.LogError($"Maze generation failed (seed {seed}): {exception.Message}", this);
            }
        }

        private List<MazeRoomView> SelectRoomPrefabs(List<MazeRoomTemplate> templates, int seed)
        {
            var selected = new List<MazeRoomView>();
            if (settings == null) return selected;
            if (settings.TreasureRoomCount < 0 || settings.SpecialRoomCount < 0)
                throw new ArgumentException("Room counts cannot be negative.");

            if (settings.TreasureRoomCount > 0 && settings.TreasureRoom3x3Prefab == null)
                throw new InvalidOperationException("Assign Treasure Room 3x3 Prefab (MazeRoomView) in MazeBalanceSettings, or set Treasure Room Count to 0.");
            if (settings.TreasureRoomCount > 0)
            {
                var template = settings.TreasureRoom3x3Prefab.CreateTemplate(MazeTileType.TreasureRoom, mazeRenderer.TileSize);
                for (var i = 0; i < settings.TreasureRoomCount; i++)
                {
                    selected.Add(settings.TreasureRoom3x3Prefab);
                    templates.Add(template);
                }
            }

            if (settings.SpecialRoomCount == 0) return selected;
            var available = new List<MazeRoomView>();
            var specialTemplates = new List<MazeRoomTemplate>();
            foreach (var prefab in settings.SpecialRoom3x3Prefabs)
            {
                if (prefab == null) continue;
                specialTemplates.Add(prefab.CreateTemplate(MazeTileType.SpecialRoom, mazeRenderer.TileSize));
                available.Add(prefab);
            }
            if (available.Count == 0)
                throw new InvalidOperationException("Assign Special Room 3x3 Prefabs (MazeRoomView) in MazeBalanceSettings, or set Special Room Count to 0.");

            var random = new System.Random(seed);
            for (var i = 0; i < settings.SpecialRoomCount; i++)
            {
                var index = random.Next(available.Count);
                selected.Add(available[index]);
                templates.Add(specialTemplates[index]);
            }
            return selected;
        }
    }
}
