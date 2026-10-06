using UnityEngine;

// 회복 포션(소모형) 스크립트
// 사용법:
// 1) RecoveryPotion 프리팹을 만들고 이 스크립트를 붙입니다.
// 2) 플레이어가 아이템을 사용하면 RecoveryPotion.Use(playerGameObject) 을 호출하세요.
//    예: Instantiate(itemPrefab); obj.GetComponent<RecoveryPotion>().Use(player);
// 3) 또는 Hotbar 등에서 아이템 사용 로직에서 직접 이 컴포넌트를 찾아 호출하면 됩니다.
public class RecoveryPotion : MonoBehaviour
{
    [Header("회복량 설정")]
    [Tooltip("사용 시 회복할 HP량")]
    public float healAmount = 20f;

    // 즉시 사용하여 플레이어의 HP를 회복합니다.
    // user 파라미터로 플레이어 GameObject를 전달하거나 null을 전달하면 씬에서 PlayerHP를 찾아 적용합니다.
    public void Use(GameObject user = null)
    {
        PlayerHP targetHP = null;

        if (user != null)
        {
            targetHP = user.GetComponent<PlayerHP>();
        }

        if (targetHP == null)
        {
            // 전달받지 못했으면 씬에서 플레이어 컴포넌트를 찾아봄
            targetHP = Object.FindFirstObjectByType<PlayerHP>();
        }

        if (targetHP == null)
        {
            Debug.LogWarning("[RecoveryPotion] PlayerHP를 찾을 수 없습니다. 회복 실패.");
            return;
        }

        if (targetHP.isDead)
        {
            Debug.Log("[RecoveryPotion] 플레이어가 사망 상태입니다. 회복 불가.");
            return;
        }

        float before = targetHP.currentHealth;
        targetHP.currentHealth = Mathf.Min(targetHP.maxHealth, targetHP.currentHealth + healAmount);
        Debug.Log($"[RecoveryPotion] HP 회복: {before} -> {targetHP.currentHealth}");

        // 소모형 아이템이면 사용 즉시 제거(프리팹 인스턴스일 경우)
        // 주의: 만약 이 스크립트가 Hotbar의 데이터 형태로만 존재한다면 Destroy는 호출하지 마세요.
        Destroy(gameObject);
    }
}

