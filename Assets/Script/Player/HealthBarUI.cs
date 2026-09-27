using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI")]
    public Slider healthSlider;
    public TMP_Text healthText;

    [Header("Player")]
    public PlayerHP playerHP;

    void Start()
    {
        healthSlider.maxValue = playerHP.maxHealth;
        healthSlider.value = playerHP.currentHealth;

        UpdateHealthText();
    }

    void Update()
    {
        healthSlider.value = playerHP.currentHealth;

        UpdateHealthText();
    }

    void UpdateHealthText()
    {
        healthText.text =
            $"   HP  |  {Mathf.CeilToInt(playerHP.currentHealth)} / {Mathf.CeilToInt(playerHP.maxHealth)}";
    }
}