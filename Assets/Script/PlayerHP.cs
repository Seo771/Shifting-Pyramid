using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    [Header("체력 설정")]
    public float maxHealth = 100f;       // 최대 체력
    public float currentHealth;          // 현재 체력

    [Header("상태 확인")]
    public bool isDead = false;          // 사망 여부

    void Start()
    {
        // 게임 시작 시 체력을 최대치로 채워둡니다.
        currentHealth = maxHealth;
    }

    void Update()
    {
        // 체력이 0 ~ maxHealth 범위를 벗어나지 않도록 방지
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // 체력이 0 이하가 되면 사망 상태로 전환
        if (currentHealth <= 0f && !isDead)
        {
            currentHealth = 0f;
            isDead = true;
            Debug.Log("💀 플레이어가 사망했습니다!");
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(20f);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"[데미지 받음] 현재 체력: {currentHealth}");
    }
}
