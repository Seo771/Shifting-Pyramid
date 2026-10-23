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
        private IReadOnlyList<MazeRoomTemplate> currentRoomTemplates;
        private IReadOnlyList<MazeRoomView> currentRoomPrefabs;
        public MazeGrid CurrentGrid => currentGrid;
        public IReadOnlyList<MazeRoomTemplate> CurrentRoomTemplates => currentRoomTemplates;
        public IReadOnlyList<MazeRoomView> CurrentRoomPrefabs => currentRoomPrefabs;
        public int CurrentSeed { get; private set; }
        public MazeBalanceSettings Settings => settings;
        public MazeRenderer Renderer => mazeRenderer;
        public Transform PlayerTransform => playerSpawner != null ? playerSpawner.PlayerTransform : null;

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
                var templateIndexes = new List<int>();
                var catalog = new List<MazeRoomTemplate>();
                var prefabs = SelectRoomPrefabs(templates, templateIndexes, catalog, seed);

                new MazeRoomPlacer().Place(grid, templates, templateIndexes, seed);
                new DepthFirstMazeGenerator().GenerateInto(grid, seed);
                var exit = new MazeExitPlacer().Place(grid);
                var targets = new List<MazeCoordinate> { exit };
                foreach (var room in grid.Rooms) targets.Add(room.Center);

                if (!new MazePathValidator().AreReachable(grid, MazeCoordinate.Zero, targets))
                    throw new InvalidOperationException("An exit or room is unreachable.");

                mazeRenderer.Build(grid, settings, seed, prefabs);
                currentGrid = grid;
                currentRoomTemplates = catalog.AsReadOnly();
                currentRoomPrefabs = prefabs.AsReadOnly();
                CurrentSeed = seed;
                if (playerSpawner == null) playerSpawner = FindFirstObjectByType<PlayerMazeSpawner>();
                if (playerSpawner != null) playerSpawner.SpawnAtStart(mazeRenderer);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                Debug.LogError($"Maze generation failed (seed {seed}): {exception.Message}", this);
            }
        }

        private List<MazeRoomView> SelectRoomPrefabs(List<MazeRoomTemplate> templates,
            List<int> templateIndexes, List<MazeRoomTemplate> catalog, int seed)
        {
            var selected = new List<MazeRoomView>();
            if (settings == null) return selected;
            if (settings.CurseJarRoomCount < 0 || settings.SpecialRoomCount < 0)
                throw new ArgumentException("Room counts cannot be negative.");

            if (settings.CurseJarRoomCount > 0 && settings.CurseJarRoom3x3Prefab == null)
                throw new InvalidOperationException("Assign Curse Jar Room 3x3 Prefab (MazeRoomView) in MazeBalanceSettings, or set Curse Jar Room Count to 0.");
            if (settings.CurseJarRoomCount > 0)
            {
                var template = settings.CurseJarRoom3x3Prefab.CreateTemplate(MazeTileType.CurseJarRoom, mazeRenderer.TileSize);
                var index = selected.Count;
                selected.Add(settings.CurseJarRoom3x3Prefab);
                catalog.Add(template);
                for (var i = 0; i < settings.CurseJarRoomCount; i++)
                {
                    templates.Add(template);
                    templateIndexes.Add(index);
                }
            }

            if (settings.SpecialRoomCount == 0) return selected;
            var available = new List<MazeRoomView>();
            foreach (var prefab in settings.SpecialRoom3x3Prefabs)
            {
                if (prefab == null) continue;
                catalog.Add(prefab.CreateTemplate(MazeTileType.SpecialRoom, mazeRenderer.TileSize));
                available.Add(prefab);
                selected.Add(prefab);
            }
            if (available.Count == 0)
                throw new InvalidOperationException("Assign Special Room 3x3 Prefabs (MazeRoomView) in MazeBalanceSettings, or set Special Room Count to 0.");

            var random = new System.Random(seed);
            for (var i = 0; i < settings.SpecialRoomCount; i++)
            {
                var index = random.Next(available.Count);
                var catalogIndex = catalog.Count - available.Count + index;
                templates.Add(catalog[catalogIndex]);
                templateIndexes.Add(catalogIndex);
            }
            return selected;
        }
    }
}
