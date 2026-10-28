
using UnityEngine;

public class TorchFireAutoPlay : MonoBehaviour
{
    private void Start()
    {
        ParticleSystem[] particles =
            GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.Play(true);
        }
    }
}
