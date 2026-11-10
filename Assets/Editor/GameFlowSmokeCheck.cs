using System;
using System.Collections.Generic;
using System.Linq;
using ShiftingPyramid.Game;
using ShiftingPyramid.Game.Map;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace ShiftingPyramid.EditorChecks
{
    /// <summary>실제 메인 씬의 승리/패배와 결과 버튼을 통한 연속 재시작 검사.</summary>
    public static class GameFlowSmokeCheck
    {
        private const string PendingKey = "ShiftingPyramid.GameFlowSmoke.Pending";
        private static DateTime deadline;
        private static DateTime triggerDeadline;
        private static int stage;
        private static int previousManagerId;
        private static int settledFrames;
        private static int errors;
        private static Action pendingResult;
        private static int resultFrames;

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode only.");
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
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }

        private static void Tick()
        {
            try
            {
                Assert(DateTime.UtcNow < deadline, "Game-flow check timed out.");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                // 비활성 결과 UI의 새 Graphic depth는 실제 UI 프레임 뒤에 확정된다.
                if (pendingResult != null)
                {
                    if (++resultFrames < 3) return;
                    var complete = pendingResult;
                    pendingResult = null;
                    complete();
                    return;
                }
                var manager = GameManager.Instance;
                var bootstrapper = Object.FindFirstObjectByType<MazeTestBootstrapper>();
                if (manager == null || bootstrapper == null || !bootstrapper.IsMazeReady) return;
                var session = manager.GetComponent<GameSessionController>();
                Assert(session != null, "MainScene must have a GameSessionController.");
                if (previousManagerId != 0 && manager.GetInstanceID() == previousManagerId) return;
                if (++settledFrames < 3) return;

                if (stage == 0)
                {
                    CheckFreshRun(manager, bootstrapper, session);
                    Assert(!manager.TryEscape(), "Locked exit must reject escape.");
                    var hp = Object.FindFirstObjectByType<PlayerHP>();
                    hp.TakeDamage(20f);
                    var hotbar = Object.FindFirstObjectByType<HotbarController>();
                    var item = new ItemData { itemName = "Pending test potion", itemType = ItemData.ItemType.Recovery,
                        healAmount = 20f, useDuration = 10f };
                    Assert(hotbar.AddItemToHotbar(item), "Test item must fit in a fresh hotbar.");
                    hotbar.UseCurrentSlot();
                    Assert(hotbar.IsUsingItem, "A delayed item use must begin before the result.");
                    CollectRequiredJars(manager);
                    var map = Object.FindFirstObjectByType<MapScreenController>();
                    map.OpenMap();
                    Assert(map.IsOpen && Time.timeScale == 0f, "Map pause must be active for the ownership test.");
                    Assert(manager.TryEscape(), "Unlocked exit must permit escape.");
                    CheckResult(manager, session, true);
                    Assert(!map.IsOpen && !MapScreenController.IsInputBlocked, "Result must close the map pause.");
                    map.OpenMap();
                    Assert(!map.IsOpen && Time.timeScale == 0f, "Result must reject reopening a paused map.");
                    Assert(!hotbar.IsUsingItem && hotbar.GetComponentsInChildren<HotbarSlot>()
                        .Any(slot => ReferenceEquals(slot.GetItem(), item)), "Result must cancel pending use without consuming the item.");
                    hp.TakeDamage(10000f);
                    hotbar.UseCurrentSlot();
                    Assert(!hp.isDead && Mathf.Approximately(hp.currentHealth, hp.maxHealth - 20f)
                        && !hotbar.IsUsingItem, "Result must reject further damage and item use.");
                    var box = Object.FindFirstObjectByType<RandomBox>();
                    box.Interact();
                    Assert(!box.IsOpened, "Result must reject box interaction.");
                    Restart(manager, session, 1);
                }
                else if (stage == 1)
                {
                    CheckFreshRun(manager, bootstrapper, session);
                    CollectRequiredJars(manager);
                    var gate = Object.FindFirstObjectByType<ExitGate>();
                    Assert(gate != null && gate.IsUnlocked, "Generated exit must be unlocked.");
                    var player = bootstrapper.PlayerTransform;
                    var controller = player.GetComponent<CharacterController>();
                    controller.enabled = false;
                    var position = gate.GetComponent<Collider>().bounds.center;
                    position.y += controller.height * 0.5f;
                    player.position = position;
                    controller.enabled = true;
                    Physics.SyncTransforms();
                    triggerDeadline = DateTime.UtcNow.AddSeconds(8);
                    stage = 2;
                }
                else if (stage == 2)
                {
                    if (!manager.IsGameFinished)
                    {
                        Assert(DateTime.UtcNow < triggerDeadline, "Physical exit trigger did not finish the run.");
                        return;
                    }
                    CheckResult(manager, session, true);
                    Restart(manager, session, 3);
                }
                else if (stage == 3)
                {
                    CheckFreshRun(manager, bootstrapper, session);
                    var hp = Object.FindFirstObjectByType<PlayerHP>();
                    hp.TakeDamage(hp.maxHealth);
                    Assert(hp.isDead, "Fatal damage must mark the player dead.");
                    CheckResult(manager, session, false);
                    Restart(manager, session, 4);
                }
                else if (stage == 4)
                {
                    CheckFreshRun(manager, bootstrapper, session);
                    var mummification = Object.FindFirstObjectByType<Mummification>();
                    mummification.IncreaseMummification(mummification.MaxValue);
                    Assert(mummification.IsMummified, "Maximum curse must mark mummification.");
                    CheckResult(manager, session, false);
                    Restart(manager, session, 5);
                }
                else
                {
                    CheckFreshRun(manager, bootstrapper, session);
                    Assert(errors == 0, $"Unexpected Unity errors: {errors}.");
                    Finish(0, "PASS: game result/input/pause, physical exit, death/mummification and four button-driven fresh restarts.");
                }
            }
            catch (Exception exception) { Finish(1, "FAIL: " + exception); }
        }

        private static void CheckFreshRun(GameManager manager, MazeTestBootstrapper bootstrapper, GameSessionController session)
        {
            Assert(manager.CurrentState == GameManager.GameState.Playing && manager.CollectedCurseJarCount == 0,
                "Restart must begin with clean progress.");
            Assert(Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1,
                "Restart must not retain a persistent duplicate manager.");
            var hp = Object.FindFirstObjectByType<PlayerHP>();
            var curse = Object.FindFirstObjectByType<Mummification>();
            Assert(!hp.isDead && Mathf.Approximately(hp.currentHealth, hp.maxHealth)
                && !curse.IsMummified && curse.CurrentValue < 1f, "Health and curse must reset with the scene.");
            Assert(!session.IsVictoryVisible && !session.IsDefeatVisible && !GameplayInput.IsBlocked
                && Mathf.Approximately(Time.timeScale, 1f), "Fresh run must restore input and time.");
            Assert(Object.FindObjectsByType<HotbarSlot>(FindObjectsSortMode.None).All(slot => slot.IsEmpty()),
                "Restart must clear inventory.");
            Assert(bootstrapper.Renderer.TryGetCoordinate(bootstrapper.PlayerTransform.position,
                bootstrapper.CurrentGrid, out var coordinate) && coordinate.Equals(MazeCoordinate.Zero),
                "Restart must place the player at the generated start.");
        }

        private static void CollectRequiredJars(GameManager manager)
        {
            foreach (var jar in Object.FindObjectsByType<CurseJar>(FindObjectsSortMode.None)) jar.Interact();
            Assert(manager.CollectedCurseJarCount >= manager.RequiredCurseJarCount && manager.IsExitUnlocked,
                "Configured jars must suffice to unlock the exit.");
        }

        private static void CheckResult(GameManager manager, GameSessionController session, bool victory)
        {
            Assert(manager.CurrentState == (victory ? GameManager.GameState.Victory : GameManager.GameState.Defeat),
                "Game result must match the ending.");
            Assert(session.IsVictoryVisible == victory && session.IsDefeatVisible != victory,
                "Only the matching result panel may be visible.");
            Assert(GameplayInput.IsBlocked && Time.timeScale == 0f && Cursor.lockState == CursorLockMode.None && Cursor.visible,
                "Result must stop play and release the cursor.");
            Assert(session.RestartButton != null && session.RestartButton.gameObject.activeInHierarchy
                && session.RestartButton.IsInteractable() && session.QuitButton != null && session.QuitButton.IsInteractable(),
                "Result buttons must be usable while time is paused.");
            var monsters = Object.FindObjectsByType<MonsterAI>(FindObjectsSortMode.None);
            Assert(monsters.All(monster => !monster.IsNavigationReady || monster.GetComponent<NavMeshAgent>().isStopped),
                "Monsters must stop immediately at the result: " + string.Join(", ", monsters.Select(monster =>
                    $"{monster.name} ready={monster.IsNavigationReady} stopped={monster.GetComponent<NavMeshAgent>().isStopped}")));
        }

        private static void CheckButtonHit(GameSessionController session)
        {
            Canvas.ForceUpdateCanvases();
            var data = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, session.RestartButton.transform.position)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == session.RestartButton,
                $"Restart button must be the top raycast target at {data.position}, screen {Screen.width}x{Screen.height}; hits: "
                + string.Join(", ", hits.Select(hit => hit.gameObject.name)));
        }

        private static void Restart(GameManager manager, GameSessionController session, int nextStage)
        {
            resultFrames = 0;
            pendingResult = () =>
            {
                CheckButtonHit(session);
                previousManagerId = manager.GetInstanceID();
                stage = nextStage;
                settledFrames = 0;
                ExecuteEvents.Execute(session.RestartButton.gameObject,
                    new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
                    ExecuteEvents.pointerClickHandler);
                Assert(session.IsRestarting, "Result button must request an asynchronous scene restart.");
            };
        }

        private static void Finish(int code, string message)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= CaptureError;
            Time.timeScale = 1f;
            Debug.Log(message);
            EditorApplication.Exit(code);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
