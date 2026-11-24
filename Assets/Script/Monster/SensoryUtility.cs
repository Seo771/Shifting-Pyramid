using UnityEngine;

public static class SensoryUtility
{
    // 상호작용 위치를 거리와 레이어 제한 없이 모든 활성 수신자에게 전달한다.
    public static void BroadcastStimulus(StimulusData stimulus)
    {
        var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var behaviour in behaviours)
        {
            if (behaviour.isActiveAndEnabled && behaviour is ISensoryReceiver receiver)
            {
                receiver.OnReceiveStimulus(stimulus);
            }
        }
    }

    // origin 위치에서 radius 반경 내에 있는 모든 ISensoryReceiver에게 자극 발송
    public static void EmitStimulus(Vector3 origin, float radius, StimulusData stimulus, LayerMask monsterLayer)
    {
        if (radius <= 0f) return;

        // Collider가 없는 MON도 감지하며, 여러 Collider를 가진 미라에도 한 번만 전달한다.
        var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var behaviour in behaviours)
        {
            if (!behaviour.isActiveAndEnabled || !(behaviour is ISensoryReceiver receiver)) continue;
            if ((monsterLayer.value & (1 << behaviour.gameObject.layer)) == 0) continue;
            if ((behaviour.transform.position - origin).sqrMagnitude > radius * radius) continue;

            receiver.OnReceiveStimulus(stimulus);
        }
    }
}
