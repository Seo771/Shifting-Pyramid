using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>미로 변경까지 남은 초를 Play Mode에서 임시로 표시한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MazeRuntimeChangeController))]
    public class MazeChangeCountdownView : MonoBehaviour
    {
        private MazeRuntimeChangeController controller;
        private Canvas overlay;
        private TextMeshProUGUI countdownText;
        private int lastSeconds = -1;

        private void Awake()
        {
            controller = GetComponent<MazeRuntimeChangeController>();
        }

        private void Start()
        {
            var canvasObject = new GameObject("Maze Change Countdown", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            overlay = canvasObject.GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var textObject = new GameObject("Seconds", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(overlay.transform, false);
            countdownText = textObject.GetComponent<TextMeshProUGUI>();
            var rect = countdownText.rectTransform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(100f, 52f);

            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.fontSize = 36f;
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.color = Color.white;
            countdownText.outlineColor = Color.black;
            countdownText.outlineWidth = 0.18f;
            countdownText.raycastTarget = false;
            countdownText.enabled = false;
        }

        private void LateUpdate()
        {
            if (countdownText == null) return;
            var seconds = controller.SecondsRemaining;
            countdownText.enabled = seconds >= 0;
            if (seconds < 0 || seconds == lastSeconds) return;

            countdownText.text = seconds.ToString();
            lastSeconds = seconds;
        }

        private void OnDestroy()
        {
            if (overlay != null) Destroy(overlay.gameObject);
        }
    }
}
