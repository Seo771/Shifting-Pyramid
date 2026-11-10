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
        private bool isApplyingUpdate;
        public MazeGrid CurrentGrid => currentGrid;
        public IReadOnlyList<MazeRoomTemplate> CurrentRoomTemplates => currentRoomTemplates;
        public IReadOnlyList<MazeRoomView> CurrentRoomPrefabs => currentRoomPrefabs;
        public int CurrentSeed { get; private set; }
        public bool IsMazeReady { get; private set; }
        public MazeUpdatePipeline Updates { get; } = new MazeUpdatePipeline();
        public event Action<MazeGrid> MazeReady;
        public event Action<MazeGrid, MazeChangeSet> MazeChanged;
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
            RebuildMaze();
        }

        // 맵 전체 재생성이다. 체력/인벤토리/승패 초기화는 새 게임 기능의 책임이다.
        public void RebuildMaze()
        {
            if (isApplyingUpdate || Updates.IsUpdating) return;
            isApplyingUpdate = true;
            try { RebuildInternal(); }
            finally { isApplyingUpdate = false; }
        }

        private void RebuildInternal()
        {
            if (mazeRenderer == null) mazeRenderer = FindFirstObjectByType<MazeRenderer>();
            if (mazeRenderer == null)
            {
                Debug.LogWarning("MazeTestBootstrapper: MazeRenderer is not assigned.", this);
                return;
            }

            var seed = settings != null && !settings.UseRandomSeed
                ? settings.Seed : new System.Random().Next();
            MazeGrid readyGrid = null;

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

                if (playerSpawner == null) playerSpawner = FindFirstObjectByType<PlayerMazeSpawner>();
                if (playerSpawner == null || !playerSpawner.HasPlayerSource)
                    throw new InvalidOperationException("Assign an existing player or player prefab before rebuilding.");

                // 새 표시가 만들어지는 동안에는 이전 미로를 준비 완료로 노출하지 않는다.
                IsMazeReady = false;
                mazeRenderer.Build(grid, settings, seed, prefabs);
                currentGrid = grid;
                currentRoomTemplates = catalog.AsReadOnly();
                currentRoomPrefabs = prefabs.AsReadOnly();
                CurrentSeed = seed;
                if (playerSpawner == null || !playerSpawner.SpawnAtStart(mazeRenderer)) return;

                Updates.Apply(grid, MazeChangeSet.Full(grid));
                IsMazeReady = true;
                readyGrid = grid;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                Debug.LogError($"Maze generation failed (seed {seed}): {exception.Message}", this);
            }

            // 구독자 예외를 미로 생성 실패로 처리하지 않는다.
            if (readyGrid != null) MazeReady?.Invoke(readyGrid);
        }

        // 데이터 검증을 통과한 부분 변경만 표시하고 후처리한다.
        public void ApplyRuntimeChange(MazeGrid proposedGrid, MazeChangeSet changes,
            MazeRoom removedRoom, MazeRoom addedRoom)
        {
            if (!IsMazeReady || isApplyingUpdate || Updates.IsUpdating || changes == null || changes.IsFullRebuild)
                throw new InvalidOperationException("Partial changes require a ready maze.");
            if (proposedGrid == null || proposedGrid.Width != currentGrid.Width
                || proposedGrid.Height != currentGrid.Height)
                throw new ArgumentException("Partial changes must keep the current grid dimensions.");
            isApplyingUpdate = true;
            try
            {
                IsMazeReady = false;
                currentGrid.CopyFrom(proposedGrid);
                if (removedRoom != null)
                    mazeRenderer.ApplyRoomReplacement(currentGrid, removedRoom, addedRoom, currentRoomPrefabs);
                mazeRenderer.Refresh(currentGrid, changes.Cells);
                Updates.Apply(currentGrid, changes);
                IsMazeReady = true;
                MazeChanged?.Invoke(currentGrid, changes);
            }
            finally { isApplyingUpdate = false; }
        }

        private List<MazeRoomView> SelectRoomPrefabs(List<MazeRoomTemplate> templates,
            List<int> templateIndexes, List<MazeRoomTemplate> catalog, int seed)
        {
            var selected = new List<MazeRoomView>();
            if (settings == null) return selected;
            if (settings.ExitRoomPrefab == null)
                throw new InvalidOperationException("Assign Exit Room Prefab in MazeBalanceSettings before playing.");
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
