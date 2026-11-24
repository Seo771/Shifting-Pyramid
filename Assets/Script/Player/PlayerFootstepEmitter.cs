using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFootstepEmitter : MonoBehaviour
{
    [Header("발소리 감지")]
    [Tooltip("걸을 때 주변 몬스터에게 발소리 위치를 전달하는 반경입니다. 0이면 감지되지 않습니다.")]
    [SerializeField, Min(0f)] private float walkDetectionRadius = 10f;
    [Tooltip("달릴 때 주변 몬스터에게 발소리 위치를 전달하는 반경입니다. 0이면 감지되지 않습니다.")]
    [SerializeField, Min(0f)] private float runDetectionRadius = 20f;
    [Tooltip("지면에서 실제로 이동한 시간을 누적해 발소리 감지 신호를 보내는 간격입니다.")]
    [SerializeField, Min(0.05f)] private float footstepInterval = 0.5f;

    private float footstepTimer;

    public void ReportMovement(Vector3 displacement, bool isGrounded, bool isMoving, bool isRunning)
    {
        displacement.y = 0f;
        if (!isActiveAndEnabled || !isGrounded || !isMoving || displacement.sqrMagnitude <= 0.000001f)
        {
            // 잠시 멈추거나 공중에 있는 동안에는 누적 이동 시간을 유지한다.
            return;
        }

        footstepTimer += Time.deltaTime;
        float interval = Mathf.Max(0.05f, footstepInterval);
        if (footstepTimer < interval) return;

        footstepTimer -= interval;
        float radius = isRunning ? runDetectionRadius : walkDetectionRadius;
        SensoryUtility.EmitStimulus(transform.position, radius,
            new StimulusData(StimulusType.Sound, transform.position, gameObject), ~0);
    }

    public void ResetFootsteps()
    {
        footstepTimer = 0f;
    }

    private void OnDisable()
    {
        ResetFootsteps();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, walkDetectionRadius));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, runDetectionRadius));
    }
}
