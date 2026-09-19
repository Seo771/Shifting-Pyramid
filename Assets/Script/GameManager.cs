using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 게임 상태
    public enum GameState
    {
        Ready,
        Playing,
        Victory,
        Defeat
    }

    [Header("Game Rules")]
    [SerializeField] private int requiredTreasureCount = 3;

    private int collectedTreasureCount;
    private bool isPlayerDead;
    private bool hasReachedExit;

    public GameState CurrentState { get; private set; } = GameState.Ready;
    public int RequiredTreasureCount => requiredTreasureCount;
    public int CollectedTreasureCount => collectedTreasureCount;
    public bool IsExitUnlocked => collectedTreasureCount >= RequiredTreasureCount;
    public bool IsGameFinished => CurrentState == GameState.Victory || CurrentState == GameState.Defeat;

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        collectedTreasureCount = 0;
        isPlayerDead = false;
        hasReachedExit = false;
        CurrentState = GameState.Playing;
    }

    // --- 획득 보물 ---
    public void AddTreasure(int amount = 1)
    {
        // 추후 보물 n개 획득시 탈출 여부 true 변경
        if (CurrentState != GameState.Playing || amount <= 0)
        {
            return;
        }

        collectedTreasureCount += amount;
    }

    // --- 탈출 여부 확인 ---
    public bool TryEscape()
    {
        if (CurrentState != GameState.Playing)
        {
            return false;
        }

        if (!IsExitUnlocked)
        {
            return false;
        }

        hasReachedExit = true;
        CheckVictoryCondition();
        return CurrentState == GameState.Victory;
    }

    public void SetPlayerDead()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        isPlayerDead = true;
        CheckDefeatCondition();
    }

    public void CheckVictoryCondition()
    {
        if (CurrentState == GameState.Playing && hasReachedExit && IsExitUnlocked)
        {
            CurrentState = GameState.Victory;
        }
    }

    public void CheckDefeatCondition()
    {
        if (CurrentState == GameState.Playing && isPlayerDead)
        {
            CurrentState = GameState.Defeat;
        }
    }
}
