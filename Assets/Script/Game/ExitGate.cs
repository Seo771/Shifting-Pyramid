using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitGate : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private string playerTag = "Player";

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

    // 컴포넌트를 처음 붙였을 때 출구 Collider를 트리거로 맞춘다.
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
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
