using System;
using System.Linq;
using ShiftingPyramid.Game.Map;
using ShiftingPyramid.Game.World;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Settings;
using ShiftingPyramid.Maze.View;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace ShiftingPyramid.EditorChecks
{
    /// <summary>명령 실행 전용. 메인 씬을 저장하지 않고 Play Mode 연결을 검사한다.</summary>
    public static class MazeLifecycleSmokeCheck
    {
        private const string PendingKey = "ShiftingPyramid.MazeLifecycleSmoke.Pending";
        private static DateTime deadline;
        private static int settledFrames;
        private static int unexpectedErrors;
        private static bool expectedError;

        public static void Run()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run this check in batch mode in a separate editor process.");
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            SessionState.SetBool(PendingKey, true);
            Resume();
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            deadline = DateTime.UtcNow.AddMinutes(3);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= CaptureError;
            Application.logMessageReceived += CaptureError;
        }

        private static void CaptureError(string message, string trace, LogType type)
        {
            if (!expectedError && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                unexpectedErrors++;
        }

        private static void Tick()
        {
            try
            {
                Assert(DateTime.UtcNow < deadline, "Timed out waiting for MainScene to become ready.");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                var bootstrapper = Object.FindFirstObjectByType<MazeTestBootstrapper>();
                if (bootstrapper == null || !bootstrapper.IsMazeReady || ++settledFrames < 3) return;
                Time.timeScale = 0f;
                CheckLifecycle(bootstrapper);
                Assert(unexpectedErrors == 0, $"Unexpected Unity errors: {unexpectedErrors}. See the log.");
                Finish(0, "PASS: Unity MainScene generation, collection, partial changes, rebuild, failure recovery and NavMesh.");
            }
            catch (Exception exception)
            {
                Finish(1, "FAIL: " + exception);
            }
        }

        private static void CheckLifecycle(MazeTestBootstrapper bootstrapper)
        {
            var originalGrid = bootstrapper.CurrentGrid;
            var missingScripts = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item.gameObject.scene.IsValid())
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
            Assert(missingScripts == 0, $"Scene contains {missingScripts} missing script components.");
            var map = Object.FindFirstObjectByType<MapStateConnector>();
            var mapState = map != null ? map.State : null;
            Assert(mapState != null, "Map state must be bound after readiness.");
            Assert(Object.FindObjectsByType<MazeFloor>(FindObjectsSortMode.None).Length > 0,
                "Migrated floor components must remain on prefab colliders.");
            var boxes = Object.FindObjectsByType<RandomBox>(FindObjectsSortMode.None);
            Assert(boxes.Length > 0, "MainScene must place at least one usable box.");
            Assert(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Any(item => item.name.StartsWith("Torch_")), "MainScene must place torches.");
            CheckNavigation(bootstrapper);

            // 런타임 복제본만 수정하며 프로젝트 설정 에셋을 저장하지 않는다.
            var settings = Object.Instantiate(bootstrapper.Settings);
            settings.hideFlags = HideFlags.DontSave;
            SetInt(settings, "seed", bootstrapper.CurrentSeed);
            SetBool(settings, "useRandomSeed", false);
            SetObject(bootstrapper, "settings", settings);
            var box = boxes.First();
            box.Interact();
            Assert(box.IsOpened && !box.gameObject.activeSelf, "Opening a box must award an item and hide it.");

            var jar = Object.FindFirstObjectByType<CurseJar>();
            Assert(jar != null && GameManager.Instance != null, "A curse jar and GameManager must exist.");
            jar.Interact();
            var collected = GameManager.Instance.CollectedCurseJarCount;
            Assert(collected == 1, "Collecting a curse jar must count once.");

            var changer = bootstrapper.GetComponent<MazeRuntimeChangeController>();
            Assert(changer != null, "Runtime change controller is required in MainScene.");
            var changed = false;
            for (var attempt = 0; attempt < 3 && !changed; attempt++) changed = changer.TryChangeNow();
            Assert(changed, "No runtime change succeeded: " + changer.LastChangeMessage);
            Assert(ReferenceEquals(originalGrid, bootstrapper.CurrentGrid), "Partial change must keep grid identity.");
            Assert(ReferenceEquals(mapState, map.State), "Partial change must keep exploration state identity.");
            Assert(box.IsOpened && !box.gameObject.activeSelf, "Partial change must not respawn an opened box.");
            Assert(GameManager.Instance.CollectedCurseJarCount == collected, "Partial change must preserve collection count.");
            CheckNavigation(bootstrapper);

            bootstrapper.RebuildMaze();
            Assert(bootstrapper.IsMazeReady && !ReferenceEquals(originalGrid, bootstrapper.CurrentGrid),
                "Full rebuild must produce a ready new grid.");
            Assert(!ReferenceEquals(mapState, map.State), "Full rebuild must replace exploration state.");
            Assert(GameManager.Instance.CollectedCurseJarCount == collected,
                "Map rebuild must not silently reset game progress.");
            Assert(Object.FindObjectsByType<RandomBox>(FindObjectsSortMode.None).Length > 0,
                "Full rebuild must place new boxes.");
            Assert(Object.FindObjectsByType<CurseJar>(FindObjectsSortMode.None).Any(item => item != jar),
                "Full rebuild must create a new curse jar instance.");
            CheckNavigation(bootstrapper);

            var savedGrid = bootstrapper.CurrentGrid;
            var savedSnapshot = savedGrid.Clone();
            var special = savedGrid.Rooms.First(room => room.Template.TileType == MazeTileType.SpecialRoom);
            var invalidRoom = new MazeRoom(special.Origin, special.Template, int.MaxValue);
            var rejected = false;
            try
            {
                bootstrapper.ApplyRuntimeChange(savedSnapshot, MazeChangeSet.Between(savedGrid, savedSnapshot), special, invalidRoom);
            }
            catch (InvalidOperationException) { rejected = true; }
            Assert(rejected && bootstrapper.IsMazeReady && MazeChangeSet.Between(savedSnapshot, savedGrid).Cells.Count == 0,
                "Invalid replacement view must be rejected before committing grid data or readiness.");

            var regionCount = settings.MazeChangeRegionCount;
            SetInt(settings, "mazeChangeRegionCount", 0);
            expectedError = true;
            try { Assert(!changer.TryChangeNow(), "Invalid change settings must fail safely."); }
            finally { expectedError = false; SetInt(settings, "mazeChangeRegionCount", regionCount); }
            Assert(bootstrapper.IsMazeReady && MazeChangeSet.Between(savedSnapshot, savedGrid).Cells.Count == 0
                && changer.LastChangedRegionCount == 0 && !string.IsNullOrEmpty(changer.LastChangeMessage),
                "Failed planning must preserve live data and report a reason without reporting stale success.");

            var exitPrefab = settings.ExitRoomPrefab;
            SetObject(settings, "exitRoomPrefab", null);
            expectedError = true;
            try { bootstrapper.RebuildMaze(); }
            finally { expectedError = false; SetObject(settings, "exitRoomPrefab", exitPrefab); }
            Assert(bootstrapper.IsMazeReady && ReferenceEquals(savedGrid, bootstrapper.CurrentGrid),
                "Preflight failure must preserve the working maze.");

            Action<MazeGrid, MazeChangeSet> fail = (_, __) => throw new InvalidOperationException("Injected smoke-test failure");
            bootstrapper.Updates.DecorationsUpdating += fail;
            expectedError = true;
            try { bootstrapper.RebuildMaze(); }
            finally { expectedError = false; bootstrapper.Updates.DecorationsUpdating -= fail; }
            Assert(!bootstrapper.IsMazeReady && !bootstrapper.Updates.IsUpdating,
                "Post-processing failure must not publish readiness or leave a reentry lock.");
            bootstrapper.RebuildMaze();
            Assert(bootstrapper.IsMazeReady, "Full rebuild must recover after a post-processing failure.");
            CheckNavigation(bootstrapper);
        }

        private static void CheckNavigation(MazeTestBootstrapper bootstrapper)
        {
            var surface = bootstrapper.Renderer.GetComponent<NavMeshSurface>();
            Assert(surface != null && surface.navMeshData != null, "Final NavMesh must be generated.");
            Assert(NavMesh.SamplePosition(bootstrapper.PlayerTransform.position, out var start, 5f, NavMesh.AllAreas),
                "The player's start must be on a navigable floor.");
            var exit = bootstrapper.CurrentGrid.Tiles.First(tile => tile.TileType == MazeTileType.Exit);
            Assert(bootstrapper.Renderer.TryGetTileWorldPosition(exit.Coordinate, out var exitPosition)
                && NavMesh.SamplePosition(exitPosition, out _, 5f, NavMesh.AllAreas),
                "Exit must have a navigable floor.");
            NavMesh.SamplePosition(exitPosition, out var end, 5f, NavMesh.AllAreas);
            var path = new NavMeshPath();
            Assert(NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete,
                "Actual prefab/NavMesh route from start to exit must be complete.");
        }

        private static void SetObject(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish(int exitCode, string message)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= CaptureError;
            Time.timeScale = 1f;
            Debug.Log(message);
            EditorApplication.Exit(exitCode);
        }
    }
}
