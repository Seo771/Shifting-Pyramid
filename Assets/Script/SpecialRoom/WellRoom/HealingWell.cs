using UnityEngine;
using UnityEngine.Events;

public class HealingWell : MonoBehaviour, IInteractable
{
    [Header("회복량 설정")]
    [Tooltip("상호작용 시 회복할 체력 수치")]
    [SerializeField] private float healthHealAmount = 50f;

    [Tooltip("상호작용 시 감소(회복)할 미라화 수치")]
    [SerializeField] private float mummificationReduceAmount = 50f;

    [Header("상태 확인")]
    [SerializeField] private bool isUsed = false;

    [Header("시각/사운드 연출 (선택)")]
    [Tooltip("우물을 사용한 후 비활성화할 오브젝트 (예: 빛나는 물 표면, 파티클 등)")]
    [SerializeField] private GameObject activeWaterVisual;

    [Tooltip("사용 완료 후 실행할 추가 이벤트 (사운드 등)")]
    public UnityEvent OnWellUsed;

    public bool IsUsed => isUsed;

    public void Interact()
    {
        // 게임 플레이 중이 아니면 무시 (RandomBox와 동일한 안전장치)
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // 이미 사용한 우물이면 무시 (1회용)
        if (isUsed)
        {
            Debug.Log("[HealingWell] 이미 사용한 우물입니다.");
            return;
        }

        // 플레이어 찾기
        PlayerHP playerHP = FindFirstObjectByType<PlayerHP>();
        Mummification mummification = FindFirstObjectByType<Mummification>();

        if (playerHP == null && mummification == null)
        {
            Debug.LogWarning("[HealingWell] 플레이어의 PlayerHP 또는 Mummification 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        // 1회용 사용 완료 처리
        isUsed = true;

        // 1. 체력 회복
        if (playerHP != null)
        {
            playerHP.Heal(healthHealAmount);
        }

        // 2. 미라화 수치 감소 (회복)
        if (mummification != null)
        {
            mummification.DecreaseMummification(mummificationReduceAmount);
            Debug.Log($"[HealingWell] 미라화 수치 -{mummificationReduceAmount} 회복 완료!");
        }

        // 3. 우물 비주얼(물/이펙트 등) 끄기
        if (activeWaterVisual != null)
        {
            activeWaterVisual.SetActive(false);
        }

        OnWellUsed?.Invoke();
    }
}