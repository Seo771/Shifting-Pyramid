using System;
using ShiftingPyramid.Game.Map;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShiftingPyramid.Game
{
    /// <summary>승패 표시, 종료 정지, 씬 재시작. 승패 규칙은 GameManager에 둔다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameManager))]
    public sealed class GameSessionController : MonoBehaviour
    {
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private RectTransform resultControlsParent;
        [SerializeField] private TMP_FontAsset resultFont;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        private GameManager manager;
        private GameObject generatedControls;
        private bool ownsResultPause;
        private bool isRestarting;

        public Button RestartButton => restartButton;
        public Button QuitButton => quitButton;
        public bool IsRestarting => isRestarting;
        public bool IsVictoryVisible => victoryPanel != null && victoryPanel.activeInHierarchy;
        public bool IsDefeatVisible => defeatPanel != null && defeatPanel.activeInHierarchy;

        private void Awake()
        {
            manager = GetComponent<GameManager>();
            CreateDefaultControls();
        }

        private void OnEnable()
        {
            manager.OnStateChanged += HandleState;
            if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            HandleState(manager.CurrentState);
        }

        private void OnDisable()
        {
            if (manager != null) manager.OnStateChanged -= HandleState;
            if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        }

        private void OnDestroy()
        {
            // 외부 씬 전환이나 Editor 정지에도 결과 화면의 정지를 남기지 않는다.
            if (ownsResultPause) Time.timeScale = 1f;
        }

        private void HandleState(GameManager.GameState state)
        {
            var finished = state == GameManager.GameState.Victory || state == GameManager.GameState.Defeat;
            if (finished && resultControlsParent != null) resultControlsParent.SetAsLastSibling();
            if (victoryPanel != null) victoryPanel.SetActive(state == GameManager.GameState.Victory);
            if (defeatPanel != null) defeatPanel.SetActive(state == GameManager.GameState.Defeat);
            if (generatedControls != null)
            {
                generatedControls.SetActive(finished);
                generatedControls.transform.SetAsLastSibling();
            }
            if (restartButton != null) restartButton.interactable = finished && !isRestarting;
            if (quitButton != null) quitButton.interactable = finished && !isRestarting;

            if (!finished)
            {
                if (ownsResultPause) Time.timeScale = 1f;
                ownsResultPause = false;
                if (state == GameManager.GameState.Playing && !MapScreenController.IsInputBlocked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return;
            }

            // 맵의 시간/커서 복원을 먼저 끝낸 뒤 결과 화면이 최종 소유권을 갖는다.
            foreach (var map in FindObjectsByType<MapScreenController>(FindObjectsSortMode.None)) map.CloseMap();
            foreach (var quiz in FindObjectsByType<SphinxQuizUI>(FindObjectsSortMode.None)) quiz.HideQuiz();
            foreach (var hotbar in FindObjectsByType<HotbarController>(FindObjectsSortMode.None)) hotbar.CancelUse();
            foreach (var monster in FindObjectsByType<MonsterAI>(FindObjectsSortMode.None)) monster.StopActions();
            ownsResultPause = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void RestartGame()
        {
            if (!manager.IsGameFinished || isRestarting) return;
            var scene = gameObject.scene;
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path)) return;
            isRestarting = true;
            if (restartButton != null) restartButton.interactable = false;
            if (quitButton != null) quitButton.interactable = false;
            Time.timeScale = 1f;
            try
            {
                if (SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Single) == null)
                    throw new InvalidOperationException("Scene reload could not be started.");
                ownsResultPause = false;
            }
            catch (Exception exception)
            {
                isRestarting = false;
                HandleState(manager.CurrentState);
                Debug.LogError($"Game restart failed: {exception.Message}", this);
            }
        }

        public void QuitGame()
        {
            if (!manager.IsGameFinished || isRestarting) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CreateDefaultControls()
        {
            if (resultControlsParent == null || restartButton != null || quitButton != null) return;
            generatedControls = new GameObject("ResultActions", typeof(RectTransform));
            generatedControls.layer = resultControlsParent.gameObject.layer;
            var root = (RectTransform)generatedControls.transform;
            root.SetParent(resultControlsParent, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(0f, -180f);
            root.sizeDelta = new Vector2(440f, 56f);
            restartButton = CreateButton(root, "Restart", "다시 시작", -110f);
            quitButton = CreateButton(root, "Quit", "게임 종료", 110f);
            generatedControls.SetActive(false);
        }

        private Button CreateButton(RectTransform parent, string name, string label, float x)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            item.layer = parent.gameObject.layer;
            var rect = (RectTransform)item.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200f, 52f);
            rect.anchoredPosition = new Vector2(x, 0f);
            var image = item.GetComponent<Image>();
            image.color = new Color(0.15f, 0.11f, 0.06f, 0.95f);
            var button = item.GetComponent<Button>();
            button.targetGraphic = image;
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = item.layer;
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            if (resultFont != null) text.font = resultFont;
            text.text = label;
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return button;
        }
    }
}
