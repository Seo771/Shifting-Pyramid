using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaminaBarUI : MonoBehaviour
{
    [Header("UI")]
    public Slider staminaSlider;
    public TMP_Text staminaText;

    [Header("Player")]
    public PlayerStamina playerStamina;

    void Start()
    {
        staminaSlider.maxValue = playerStamina.maxStamina;
        staminaSlider.value = playerStamina.currentStamina;

        UpdateStaminaText();
    }

    void Update()
    {
        staminaSlider.value = playerStamina.currentStamina;

        UpdateStaminaText();
    }

    void UpdateStaminaText()
    {
        staminaText.text =
            $"STAM | {Mathf.CeilToInt(playerStamina.currentStamina)} / {Mathf.CeilToInt(playerStamina.maxStamina)}";
    }
}