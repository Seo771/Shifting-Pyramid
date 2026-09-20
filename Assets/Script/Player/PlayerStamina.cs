using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("스테미나 설정")]
    public float maxStamina = 100f;       // 최대 스테미나
    public float currentStamina;          // 현재 스테미나
    public float staminaDrainRate = 20f;  // 달릴 때 초당 소모량
    public float staminaRegenRate = 15f;  // 쉴 때 초당 회복량

    public bool isExhausted = false;

    [Header("회복 딜레이")]
    public float regenDelay = 1f;         // 달리기를 멈추고 회복 시작까지 걸리는 시간
    private float delayTimer;
    void Start()
    {
        // 시작할 때 스테미나를 최대치로 채웁니다.
        currentStamina = maxStamina;
    }
    void Update()
    {
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    // 1. 달릴 때 호출하여 스테미나를 줄이는 함수
    public void DrainStamina()
    {
        if (currentStamina > 0)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;

            // 스테미나가 0 이하가 되면 '탈진 상태'로 전환!
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true;
            }
        }
    }
    // 2. 달리지 않을 때(걷거나 멈춤) 호출하여 스테미나를 회복하는 함수
    public void RegenerateStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;

            // 스테미나가 다시 100% (최대치) 다 차면 '탈진 상태' 해제!
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                isExhausted = false;
            }
        }
    }

    // 3. 달릴 수 있는 상태인지 확인하는 함수
    public bool CanRun()
    {
        // 스테미나가 0보다 크더라도, 탈진 상태(isExhausted)라면 달릴 수 없음!
        return currentStamina > 0f && !isExhausted;
    }
}
