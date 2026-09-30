using ShiftingPyramid.Maze.Settings;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 어디서든 GameManager.Instance로 접근
    public static GameManager Instance { get; private set; }

    // 에디터 및 외부에서 쉽게 접근 가능한 보물 개수 변수 (요구사항)
    [Tooltip("획득한 보물의 개수")]
    public int treasureCount = 0;
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
    public int RequiredTreasureCount => fallbackRequiredTreasureCount;
    public int CollectedTreasureCount => collectedTreasureCount;
    public bool IsExitUnlocked => collectedTreasureCount >= RequiredTreasureCount;
    public bool IsGameFinished => CurrentState == GameState.Victory || CurrentState == GameState.Defeat;

    // 씬 시작 시 바로 플레이 상태로 전환한다.
    private void Start()
    {
        StartGame();
    }

    private void Awake()
    {
        // 싱글톤 초기화: 중복 인스턴스가 있으면 파괴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 새 게임 시작 또는 재시작 시 필요한 값을 초기화한다.
    public void StartGame()
    {
        collectedTreasureCount = 0;
        isPlayerDead = false;
        hasReachedExit = false;
        CurrentState = GameState.Playing;
        // 외부 요구사항: 공개된 treasureCount 변수도 동기화
        treasureCount = 0;
    }

    // 보물 획득 처리는 나중에 Treasure 관련 스크립트에서 이 함수를 호출한다.
    // 요구사항: 인자 없는 호출을 지원하는 public void AddTreasure()
    public void AddTreasure()
    {
        AddTreasure(1);
    }

    // 실제 보물 개수를 증감하는 메서드(외부에서 특정 개수만큼 늘릴 때 사용)
    public void AddTreasure(int amount)
    {
        if (CurrentState != GameState.Playing || amount <= 0)
        {
            return;
        }

        collectedTreasureCount += amount;
        // 공개 변수와 동기화 및 디버그 출력
        treasureCount += amount;
        Debug.Log($"[GameManager] 보물 획득! 현재 보물 개수: {treasureCount}");
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
