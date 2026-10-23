using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class GameNotice : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private string curseMessage = "저주를 해제하였습니다.";
    [SerializeField] private string exitMessage = "출구가 열렸습니다!";
    [SerializeField, Min(0.1f)] private float visibleSeconds = 3f;

    private readonly Queue<string> pendingMessages = new Queue<string>();
    private GameManager gameManager;
    private Coroutine displayRoutine;
    private int lastCollectedCount;

    private void Reset()
    {
        messageText = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (messageText == null) messageText = GetComponent<TMP_Text>();
        Hide();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnCurseJarChanged -= HandleCurseJarChanged;
            gameManager.OnExitUnlocked -= HandleExitUnlocked;
        }
        gameManager = null;
        if (displayRoutine != null) StopCoroutine(displayRoutine);
        displayRoutine = null;
        pendingMessages.Clear();
        Hide();
    }

    private void Subscribe()
    {
        if (gameManager != null) return;
        gameManager = GameManager.Instance != null
            ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        if (gameManager == null) return;

        lastCollectedCount = gameManager.CollectedCurseJarCount;
        gameManager.OnCurseJarChanged += HandleCurseJarChanged;
        gameManager.OnExitUnlocked += HandleExitUnlocked;
    }

    private void HandleCurseJarChanged(int collectedCount, int requiredCount)
    {
        if (collectedCount > lastCollectedCount) Enqueue(curseMessage);
        lastCollectedCount = collectedCount;
    }

    private void HandleExitUnlocked()
    {
        Enqueue(exitMessage);
    }

    private void Enqueue(string message)
    {
        if (messageText == null)
        {
            Debug.LogWarning("GameNotice: TMP text is not assigned.", this);
            return;
        }

        pendingMessages.Enqueue(message);
        if (displayRoutine == null) displayRoutine = StartCoroutine(ShowQueuedMessages());
    }

    private IEnumerator ShowQueuedMessages()
    {
        // 마지막 저주와 출구 알림이 같은 프레임에 와도 차례대로 보여준다.
        while (pendingMessages.Count > 0)
        {
            messageText.text = pendingMessages.Dequeue();
            messageText.enabled = true;
            yield return new WaitForSecondsRealtime(visibleSeconds);
        }

        Hide();
        displayRoutine = null;
    }

    private void Hide()
    {
        if (messageText != null) messageText.enabled = false;
    }
}
