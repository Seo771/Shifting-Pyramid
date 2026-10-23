using ShiftingPyramid.Maze.Settings;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 어디서든 GameManager.Instance로 접근
    public static GameManager Instance { get; private set; }

    public event Action<int, int> OnCurseJarChanged;
    public event Action OnExitUnlocked;

    // 에디터 및 외부에서 쉽게 접근 가능한 저주 단지 개수 변수 (요구사항)
    [Tooltip("획득한 저주 단지의 개수")]
    [FormerlySerializedAs("treasureCount")]
    public int curseJarCount = 0;
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
    [FormerlySerializedAs("fallbackRequiredTreasureCount")]
    [SerializeField] private int fallbackRequiredCurseJarCount = 4;
    [SerializeField, Min(-1), Tooltip("테스트용 출구 개방 개수. -1이면 미로 설정 값을 사용합니다.")]
    private int exitUnlockCountOverride = -1;

    [Header("Game Result")]
    [SerializeField] private UnityEvent onVictory = new UnityEvent();
    [SerializeField] private UnityEvent onDefeat = new UnityEvent();

    // 저주 단지 개수와 승패 판정에 필요한 최소 상태만 관리한다.
    private int collectedCurseJarCount;
    private bool isPlayerDead;
    private bool hasReachedExit;
    private bool hasUnlockedExit;

    public GameState CurrentState { get; private set; } = GameState.Ready;
    public int RequiredCurseJarCount => exitUnlockCountOverride >= 0
        ? exitUnlockCountOverride
        : mazeBalanceSettings != null ? mazeBalanceSettings.CurseJarRoomCount : fallbackRequiredCurseJarCount;
    public int CollectedCurseJarCount => collectedCurseJarCount;
    public bool IsExitUnlocked => hasUnlockedExit;
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
        collectedCurseJarCount = 0;
        isPlayerDead = false;
        hasReachedExit = false;
        hasUnlockedExit = RequiredCurseJarCount <= 0;
        CurrentState = GameState.Playing;
        // 외부 요구사항: 공개된 curseJarCount 변수도 동기화
        curseJarCount = 0;

        OnCurseJarChanged?.Invoke(collectedCurseJarCount, RequiredCurseJarCount);
        if (hasUnlockedExit)
        {
            OnExitUnlocked?.Invoke();
        }
    }

    // 저주 단지 획득 처리는 나중에 CurseJar 관련 스크립트에서 이 함수를 호출한다.
    // 요구사항: 인자 없는 호출을 지원하는 public void AddCurseJar()
    public void AddCurseJar()
    {
        AddCurseJar(1);
    }

    // 실제 저주 단지 개수를 증감하는 메서드(외부에서 특정 개수만큼 늘릴 때 사용)
    public void AddCurseJar(int amount)
    {
        if (CurrentState != GameState.Playing || amount <= 0)
        {
            return;
        }

        collectedCurseJarCount += amount;
        // 공개 변수와 동기화 및 디버그 출력
        curseJarCount += amount;
        OnCurseJarChanged?.Invoke(collectedCurseJarCount, RequiredCurseJarCount);

        if (!hasUnlockedExit && collectedCurseJarCount >= RequiredCurseJarCount)
        {
            UnlockExit();
        }

        Debug.Log($"[GameManager] 저주 단지 획득! 현재 저주 단지 개수: {curseJarCount}");
    }

    private void UnlockExit()
    {
        hasUnlockedExit = true;
        OnExitUnlocked?.Invoke();
        Debug.Log("[GameManager] 출구가 열렸습니다.");
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

    // 출구 도달 + 저주 단지 조건 충족이면 승리한다.
    public void CheckVictoryCondition()
    {
        if (CurrentState == GameState.Playing && hasReachedExit && IsExitUnlocked)
        {
            CurrentState = GameState.Victory;
            onVictory?.Invoke();
        }
    }

    // 플레이어 사망 상태면 패배한다.
    public void CheckDefeatCondition()
    {
        if (CurrentState == GameState.Playing && isPlayerDead)
        {
            CurrentState = GameState.Defeat;
            onDefeat?.Invoke();
        }
    }
}
