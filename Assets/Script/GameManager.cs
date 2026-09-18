using ShiftingPyramid.Maze.Settings;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 전체 게임 진행 상태
    public enum GameState
    {
        Ready,
        Playing,
        Victory,
        Defeat
    }

    [Header("Balance Settings")]
    [SerializeField] private MazeBalanceSettings mazeBalanceSettings;
    [SerializeField] private int fallbackRequiredTreasureCount = 3;

    // 보물 개수와 승패 판정에 필요한 최소 상태만 관리한다.
    private int collectedTreasureCount;
    private bool isPlayerDead;
    private bool hasReachedExit;

    public GameState CurrentState { get; private set; } = GameState.Ready;
    public int RequiredTreasureCount => mazeBalanceSettings != null
        ? mazeBalanceSettings.RequiredTreasureCount
        : fallbackRequiredTreasureCount;
    public int CollectedTreasureCount => collectedTreasureCount;
    public bool IsExitUnlocked => collectedTreasureCount >= RequiredTreasureCount;
    public bool IsGameFinished => CurrentState == GameState.Victory || CurrentState == GameState.Defeat;

    // 씬 시작 시 바로 플레이 상태로 전환한다.
    private void Start()
    {
        StartGame();
    }

    // 새 게임 시작 또는 재시작 시 필요한 값을 초기화한다.
    public void StartGame()
    {
        collectedTreasureCount = 0;
        isPlayerDead = false;
        hasReachedExit = false;
        CurrentState = GameState.Playing;
    }

    // 보물 획득 처리는 나중에 Treasure 관련 스크립트에서 이 함수를 호출한다.
    public void AddTreasure(int amount = 1)
    {
        if (CurrentState != GameState.Playing || amount <= 0)
        {
            return;
        }

        collectedTreasureCount += amount;
    }

    // 출구에 닿았을 때 호출한다. 출구가 잠겨 있으면 탈출 실패로 처리한다.
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

    // 플레이어가 죽었을 때 호출한다.
    public void SetPlayerDead()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        isPlayerDead = true;
        CheckDefeatCondition();
    }

    // 출구 도달 + 보물 조건 충족이면 승리한다.
    public void CheckVictoryCondition()
    {
        if (CurrentState == GameState.Playing && hasReachedExit && IsExitUnlocked)
        {
            CurrentState = GameState.Victory;
        }
    }

    // 플레이어 사망 상태면 패배한다.
    public void CheckDefeatCondition()
    {
        if (CurrentState == GameState.Playing && isPlayerDead)
        {
            CurrentState = GameState.Defeat;
        }
    }
}
