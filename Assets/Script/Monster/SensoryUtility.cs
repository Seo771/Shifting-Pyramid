using UnityEngine;

public static class SensoryUtility
{
    // origin 위치에서 radius 반경 내에 있는 모든 ISensoryReceiver에게 자극 발송
    public static void EmitStimulus(Vector3 origin, float radius, StimulusData stimulus, LayerMask monsterLayer)
    {
        Collider[] colliders = Physics.OverlapSphere(origin, radius, monsterLayer);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent<ISensoryReceiver>(out var receiver))
            {
                receiver.OnReceiveStimulus(stimulus);
            }
        }
    }
}