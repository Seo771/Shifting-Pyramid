
using UnityEngine;

public class TorchLight : MonoBehaviour
{
    [Header("Torch Light Settings")]
    public float lightRange = 8f;
    public float lightIntensity = 2f;
    public Color lightColor = new Color(1f, 0.55f, 0.2f);

    [Header("Flicker Settings")]
    public bool useFlicker = true;
    public float flickerSpeed = 8f;
    public float flickerAmount = 0.2f;

    private Light torchLight;
    private float baseIntensity;

    void Start()
    {
        Transform fire = transform.Find("TorchFire");

        if (fire == null)
        {
            Debug.LogError($"{name}: TorchFire를 찾을 수 없습니다.");
            return;
        }

        torchLight = fire.GetComponent<Light>();

        if (torchLight == null)
            torchLight = fire.gameObject.AddComponent<Light>();

        torchLight.type = LightType.Point;
        torchLight.range = lightRange;
        torchLight.intensity = lightIntensity;
        torchLight.color = lightColor;
        torchLight.enabled = true;

        baseIntensity = lightIntensity;

        Debug.Log($"{name}: Torch Light 생성 완료");
    }

    void Update()
    {
        if (torchLight == null || !useFlicker)
            return;

        float flicker = Mathf.PerlinNoise(
            Time.time * flickerSpeed, 0f
        );

        torchLight.intensity =
            baseIntensity + (flicker - 0.5f) * flickerAmount;
    }
}
