using TMPro;
using UnityEngine;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>Displays the remaining seconds in the assigned canvas text.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MazeRuntimeChangeController))]
    public class MazeChangeCountdownView : MonoBehaviour
    {
        [SerializeField] private TMP_Text countdownText;
        private MazeRuntimeChangeController controller;

        private void Awake()
        {
            controller = GetComponent<MazeRuntimeChangeController>();
        }

        private void LateUpdate()
        {
            if (countdownText == null) return;
            int seconds = controller.SecondsRemaining;
            countdownText.text = seconds >= 0 ? seconds.ToString() : string.Empty;
        }
    }
}
