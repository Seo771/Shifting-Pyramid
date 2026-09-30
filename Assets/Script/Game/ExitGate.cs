using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitGate : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject[] lockedVisuals;
    [SerializeField] private GameObject[] unlockedVisuals;
    [SerializeField] private Collider[] blockingColliders;

    // GameManager 기준으로 현재 출구가 열려 있는지 확인한다.
    public bool IsUnlocked => gameManager != null && gameManager.IsExitUnlocked;

    // 인스펙터에 GameManager를 연결하지 않았을 때 씬에서 자동으로 찾는다.
    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
        RefreshState();
    }

    private void Start()
    {
        SubscribeToGameManager();
        RefreshState();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
    }

    // 컴포넌트를 처음 붙였을 때 출구 Collider를 트리거로 맞춘다.
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void SubscribeToGameManager()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null
                ? GameManager.Instance
                : FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            return;
        }

        UnsubscribeFromGameManager();
        gameManager.OnTreasureChanged += HandleTreasureChanged;
        gameManager.OnExitUnlocked += HandleExitUnlocked;
    }

    private void UnsubscribeFromGameManager()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnTreasureChanged -= HandleTreasureChanged;
        gameManager.OnExitUnlocked -= HandleExitUnlocked;
    }

    private void HandleTreasureChanged(int collectedCount, int requiredCount)
    {
        RefreshState();
    }

    private void HandleExitUnlocked()
    {
        RefreshState();
    }

    private void RefreshState()
    {
        bool isUnlocked = IsUnlocked;

        SetActive(lockedVisuals, !isUnlocked);
        SetActive(unlockedVisuals, isUnlocked);
        SetEnabled(blockingColliders, !isUnlocked);
    }

    private void SetActive(GameObject[] targets, bool isActive)
    {
        if (targets == null)
        {
            return;
        }

        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }
        }
    }

    private void SetEnabled(Collider[] targets, bool isEnabled)
    {
        if (targets == null)
        {
            return;
        }

        foreach (Collider target in targets)
        {
            if (target != null)
            {
                target.enabled = isEnabled;
            }
        }
    }

    // 플레이어가 출구에 닿으면 GameManager에 탈출 시도를 요청한다.
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || gameManager == null)
        {
            return;
        }

        gameManager.TryEscape();
    }
}
