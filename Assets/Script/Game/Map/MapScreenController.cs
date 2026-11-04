using UnityEngine;
using UnityEngine.UI;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>M 키와 닫기 버튼으로 전체 맵과 일시정지를 관리한다.</summary>
    [DisallowMultipleComponent]
    public sealed class MapScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject fullMap;

        private Button closeButton;
        private bool isOpen;
        private float previousTimeScale;
        private CursorLockMode previousLockState;
        private bool previousCursorVisible;

        public bool IsOpen => isOpen;
        public static bool IsInputBlocked { get; private set; }

        private void Awake()
        {
            if (fullMap != null) fullMap.SetActive(false);
        }

        private void OnEnable()
        {
            if (fullMap == null) return;
            closeButton = fullMap.GetComponentInChildren<Button>(true);
            if (closeButton != null) closeButton.onClick.AddListener(CloseMap);
        }

        private void Update()
        {
            if (isOpen && GameManager.Instance != null && GameManager.Instance.IsGameFinished)
            {
                CloseMap();
                return;
            }

            // Update는 timeScale이 0이어도 실행되므로 M 키로 다시 닫을 수 있다.
            if (!Input.GetKeyDown(KeyCode.M)) return;
            if (isOpen) CloseMap();
            else if (GameManager.Instance == null
                || GameManager.Instance.CurrentState == GameManager.GameState.Playing) OpenMap();
        }

        private void OnDisable()
        {
            CloseMap();
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseMap);
            closeButton = null;
        }

        private void OpenMap()
        {
            if (isOpen || fullMap == null) return;
            previousTimeScale = Time.timeScale;
            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            isOpen = true;
            IsInputBlocked = true;
            fullMap.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseMap()
        {
            if (!isOpen) return;
            isOpen = false;
            IsInputBlocked = false;
            if (fullMap != null) fullMap.SetActive(false);
            // 맵을 열기 전의 시간과 커서 상태만 복원한다.
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousLockState;
            Cursor.visible = previousCursorVisible;
        }
    }
}
