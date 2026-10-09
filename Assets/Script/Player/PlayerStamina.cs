using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("스테미나 설정")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 15f;

    [Header("탈진 상태")]
    public bool isExhausted = false;

    [Header("회복 딜레이")]
    public float regenDelay = 1f;
    private float delayTimer;

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        // 스태미나가 0이 되면 탈진 설정
        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isExhausted = true;
        }

        // ★ 스태미나가 정확히 100% 채워졌을 때만 탈진 해제
        if (isExhausted)
        {
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                isExhausted = false;
            }
        }
    }

    public void DrainStamina()
    {
        // 탈진 상태일 때는 소모 로직 실행 안 함
        if (isExhausted) return;

        delayTimer = regenDelay;

        if (currentStamina > 0f)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
            }
        }
    }

    public void RegenerateStamina()
    {
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
            return;
        }

        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
        }
    }

    public bool CanRun()
    {
        // ★ 탈진 상태(isExhausted)이거나 스태미나가 0 이하이면 무조건 false
        if (isExhausted || currentStamina <= 0f)
        {
            return false;
        }

        return true;
    }
}