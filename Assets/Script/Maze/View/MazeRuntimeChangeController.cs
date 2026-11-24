using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.RuntimeChange;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>게임 중 특수방과 일반 통로를 주기적으로 변경한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MazeTestBootstrapper))]
    [DefaultExecutionOrder(100)] // MouseLook의 LateUpdate 뒤에 회전 진동을 합성한다.
    public class MazeRuntimeChangeController : MonoBehaviour
    {
        [SerializeField] private Transform mummy;

        [Header("Maze Change Feedback")]
        [SerializeField, Min(0f), Tooltip("변경 몇 초 전부터 예고 연출을 시작할지 설정합니다.")]
        private float warningLeadTime = 1f;
        [SerializeField, Tooltip("비워 두면 플레이어의 카메라를 자동으로 찾습니다.")]
        private Camera feedbackCamera;
        [SerializeField, Min(0f)] private float warningShakeStrength = 0.12f;
        [SerializeField, Min(0f)] private float changeShakeStrength = 0.4f;
        [SerializeField, Min(0f), Tooltip("변경 순간 카메라의 최대 회전 진동 각도입니다. 0이면 위치만 흔듭니다.")]
        private float shakeRotationDegrees = 3f;
        [SerializeField, Min(0f)] private float changeShakeDuration = 1.5f;
        [SerializeField, Min(0f)] private float shakeFrequency = 16f;

        [Header("Maze Change Sound")]
        [SerializeField, Tooltip("변경 직전의 낮은 울림. 비워 두어도 됩니다.")]
        private AudioClip warningSound;
        [SerializeField, Tooltip("실제 변경 성공 순간의 효과음. 비워 두어도 됩니다.")]
        private AudioClip changeSound;
        [SerializeField, Range(0f, 1f)] private float warningVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float changeVolume = 0.8f;

        private readonly MazeRegionChanger changer = new MazeRegionChanger();
        private readonly SpecialRoomReplacer roomReplacer = new SpecialRoomReplacer();
        private MazeTestBootstrapper bootstrapper;
        private MazeGrid activeGrid;
        private float timeUntilChange;
        private int changeCount;
        private bool warningStarted;
        private bool impactActive;
        private float impactTimeRemaining;
        private AudioSource feedbackAudio;
        private bool audioPaused;
        private Transform shakenCamera;
        private Vector3 cameraRestPosition;
        private Quaternion cameraRestRotation;
        public int LastChangedRegionCount { get; private set; }
        public string LastChangeMessage { get; private set; }

        public int SecondsRemaining => activeGrid == null
            ? -1 : Mathf.CeilToInt(Mathf.Max(0f, timeUntilChange));

        private void Awake()
        {
            bootstrapper = GetComponent<MazeTestBootstrapper>();
        }

        private void Update()
        {
            // 이전 프레임의 진동을 먼저 제거한다. 이후 MouseLook이 새 시점을 계산한다.
            RestoreCamera();
            var grid = bootstrapper.CurrentGrid;
            var settings = bootstrapper.Settings;
            var renderer = bootstrapper.Renderer;
            var player = bootstrapper.PlayerTransform;
            if (!bootstrapper.IsMazeReady || grid == null || settings == null || renderer == null || player == null) return;

            if (!ReferenceEquals(activeGrid, grid))
            {
                StopFeedback();
                warningStarted = false;
                activeGrid = grid;
                timeUntilChange = settings.MazeChangeInterval;
                changeCount = 0;
                LastChangedRegionCount = 0;
                LastChangeMessage = null;
            }

            if (GameManager.Instance != null
                && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
            if (!renderer.TryGetCoordinate(player.position, grid, out var playerCoordinate)) return;

            timeUntilChange -= Time.deltaTime;
            if (!warningStarted && warningLeadTime > 0f
                && timeUntilChange > 0f && timeUntilChange <= warningLeadTime)
            {
                warningStarted = true;
                PlayFeedbackSound(warningSound, warningVolume);
            }
            if (timeUntilChange > 0f) return;
            TryChangeNow();
        }

        [ContextMenu("Try Change Maze Now")]
        public void ChangeNow()
        {
            TryChangeNow();
        }

        // 타이머와 수동 검사가 같은 경로를 사용한다. 실패는 다음 주기에 재시도한다.
        public bool TryChangeNow()
        {
            StopFeedback();
            warningStarted = false;
            if (bootstrapper == null) bootstrapper = GetComponent<MazeTestBootstrapper>();
            LastChangedRegionCount = 0;
            LastChangeMessage = null;
            var grid = bootstrapper.CurrentGrid;
            var settings = bootstrapper.Settings;
            var renderer = bootstrapper.Renderer;
            var player = bootstrapper.PlayerTransform;
            if (!bootstrapper.IsMazeReady || grid == null || settings == null || renderer == null || player == null)
                return Skip("Maze is not ready.");
            if (GameManager.Instance != null
                && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
                return Skip("Game is not playing.");
            if (!renderer.TryGetCoordinate(player.position, grid, out var playerCoordinate))
                return Skip("Player is outside the maze.");
            timeUntilChange = settings.MazeChangeInterval;

            try
            {
                // 모든 활성 미라를 보호한다. 기존 Inspector 참조도 계속 지원한다.
                var protectedAreas = new List<MazeProtectedArea>();
                foreach (var monster in FindObjectsByType<MonsterAI>(FindObjectsSortMode.None))
                    if (monster.isActiveAndEnabled && !AddProtectedArea(monster.transform, grid, renderer,
                        settings.MummySafeRadius, protectedAreas)) return false;
                if (mummy != null && !AddProtectedArea(mummy, grid, renderer,
                    settings.MummySafeRadius, protectedAreas)) return false;

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
                if (!roomChanged && regions.Count == 0) return Skip("No safe structural change was found.");
                bootstrapper.ApplyRuntimeChange(proposed, MazeChangeSet.Between(grid, proposed), removedRoom, addedRoom);
                LastChangedRegionCount = regions.Count;
                LastChangeMessage = $"Changed {regions.Count} corridor regions; special room replaced: {roomChanged}.";
                impactActive = true;
                impactTimeRemaining = changeShakeDuration;
                PlayFeedbackSound(changeSound, changeVolume);
                return true;
            }
            catch (Exception exception)
            {
                LastChangeMessage = exception.Message;
                Debug.LogError($"Maze change failed: {exception.Message}", this);
                return false;
            }
        }

        private void LateUpdate()
        {
            // MouseLook이 계산한 이번 프레임 시점에 위치·회전 진동을 덧붙인다.
            if (bootstrapper == null || !bootstrapper.IsMazeReady
                || !ReferenceEquals(activeGrid, bootstrapper.CurrentGrid)
                || GameManager.Instance != null
                    && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                StopFeedback();
                return;
            }

            if (Time.timeScale <= 0f)
            {
                if (feedbackAudio != null && feedbackAudio.isPlaying)
                {
                    feedbackAudio.Pause();
                    audioPaused = true;
                }
                return;
            }
            if (audioPaused)
            {
                if (feedbackAudio != null) feedbackAudio.UnPause();
                audioPaused = false;
            }

            float strength = 0f;
            if (impactActive)
            {
                impactTimeRemaining = Mathf.Max(0f, impactTimeRemaining - Time.deltaTime);
                float progress = changeShakeDuration > 0f
                    ? 1f - impactTimeRemaining / changeShakeDuration : 1f;
                // 초반 35%는 충격을 유지하고, 나머지 구간에서 부드럽게 감쇠한다.
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, progress));
                strength = changeShakeStrength * (1f - fade);
                if (impactTimeRemaining <= 0f) impactActive = false;
            }
            else if (warningStarted && warningLeadTime > 0f)
            {
                strength = warningShakeStrength
                    * Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(1f - timeUntilChange / warningLeadTime));
            }
            if (strength <= 0f) return;

            if (feedbackCamera == null)
            {
                var player = bootstrapper.PlayerTransform;
                feedbackCamera = player != null ? player.GetComponentInChildren<Camera>() : null;
                if (feedbackCamera == null) feedbackCamera = Camera.main;
            }
            if (feedbackCamera == null) return;

            shakenCamera = feedbackCamera.transform;
            cameraRestPosition = shakenCamera.localPosition;
            cameraRestRotation = shakenCamera.localRotation;
            float sample = Time.time * shakeFrequency;
            var noise = new Vector3(
                Mathf.PerlinNoise(sample, 0f) * 2f - 1f,
                Mathf.PerlinNoise(0f, sample + 17f) * 2f - 1f, 0f);
            shakenCamera.localPosition = cameraRestPosition + noise * strength;
            float rotation = shakeRotationDegrees * strength
                / Mathf.Max(changeShakeStrength, Mathf.Max(warningShakeStrength, 0.001f));
            shakenCamera.localRotation = cameraRestRotation
                * Quaternion.Euler(noise.y * rotation, noise.x * rotation * 0.35f, noise.x * rotation);
        }

        private void PlayFeedbackSound(AudioClip clip, float volume)
        {
            if (clip == null) return;
            if (feedbackAudio == null)
            {
                // 다른 효과음과 Stop/Pause가 간섭하지 않도록 전용 소스를 만든다.
                feedbackAudio = gameObject.AddComponent<AudioSource>();
                feedbackAudio.playOnAwake = false;
                feedbackAudio.spatialBlend = 0f;
                feedbackAudio.loop = false;
            }
            feedbackAudio.PlayOneShot(clip, volume);
        }

        private void RestoreCamera()
        {
            if (shakenCamera != null)
            {
                shakenCamera.localPosition = cameraRestPosition;
                shakenCamera.localRotation = cameraRestRotation;
            }
            shakenCamera = null;
        }

        private void StopFeedback()
        {
            RestoreCamera();
            impactActive = false;
            impactTimeRemaining = 0f;
            if (feedbackAudio != null) feedbackAudio.Stop();
            audioPaused = false;
        }

        private void OnDisable()
        {
            StopFeedback();
            warningStarted = false;
        }

        private bool Skip(string message)
        {
            LastChangeMessage = message;
            return false;
        }

        private bool AddProtectedArea(Transform character, MazeGrid grid, MazeRenderer renderer,
            int radius, List<MazeProtectedArea> areas)
        {
            if (!renderer.TryGetCoordinate(character.position, grid, out var cell))
            {
                LastChangeMessage = $"'{character.name}' is outside the maze.";
                Debug.LogWarning($"Maze change skipped: {LastChangeMessage}", this);
                return false;
            }
            areas.Add(new MazeProtectedArea(cell, radius));
            return true;
        }
    }
}
