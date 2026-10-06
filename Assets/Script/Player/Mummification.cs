using UnityEngine;
using UnityEngine.Events;

public class Mummification : MonoBehaviour
{
    [Header("미라화 설정")]
    [Tooltip("초당 증가하는 미라화 수치")]
    [SerializeField] private float increaseRatePerSecond = 1f;

    // 내부 고정값 (인스펙터 노출 X)
    private readonly float maxMummification = 100f;
    [Header("현재 상태 (확인용)")]
    [SerializeField] private float currentMummification = 0f;
    private bool isMummified = false;

    // 외부 공개용 프로퍼티 (읽기 전용)
    public float CurrentValue => currentMummification;
    public float MaxValue => maxMummification;
    public float NormalizedValue => currentMummification / maxMummification; // 0.0 ~ 1.0 (UI 슬라이더용)
    public bool IsMummified => isMummified;

    // 이벤트 (UI 및 상태 변화 방송용)
    public UnityEvent<float, float> onMummificationChanged; // (현재 값, 최대 값)
    public UnityEvent onMummified;                         // 100% 달성 시 신호

    private void Update()
    {
        if (isMummified) return;

        // 초당 수치 증가
        IncreaseMummification(increaseRatePerSecond * Time.deltaTime);
    }

    /// <summary>
    /// 미라화 수치를 특정 양만큼 증가시키는 함수
    /// </summary>
    public void IncreaseMummification(float amount)
    {
        if (isMummified) return;

        currentMummification += amount;
        currentMummification = Mathf.Clamp(currentMummification, 0f, maxMummification);

        // 값 변경 알림 방송
        onMummificationChanged?.Invoke(currentMummification, maxMummification);

        // 100% 도달 시
        if (currentMummification >= maxMummification)
        {
            TriggerMummification();
        }
    }

    /// <summary>
    /// 100%가 되었을 때 처리
    /// </summary>
    private void TriggerMummification()
    {
        isMummified = true;
        Debug.Log("[경고] 미라화 수치 100% 도달! 플레이어가 미라가 되었습니다.");

        onMummified?.Invoke();
    }
}