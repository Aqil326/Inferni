using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField]
    private Image healthBar;

    [SerializeField]
    private Image deathTick;

    [SerializeField]
    private TextMeshProUGUI healthAmount;

    private Character character;

    public void Init(Character character)
    {
        this.character = character;
        character.Health.OnValueChanged += OnUpdateHealth;
        character.MaxHealth.OnValueChanged += OnUpdateMaxHealth;
        OnUpdateHealth(character.Health.Value, character.Health.Value);
    }

    private void OnUpdateMaxHealth(int oldMaxHealth, int newMaxHealth)
    {
        UpdateHealthUI(character.Health.Value, newMaxHealth);
    }

    private void OnUpdateHealth(int oldHealth, int newHealth)
    {
        UpdateHealthUI(newHealth, character.MaxHealth.Value);
    }

    private void UpdateHealthUI(int health, int maxHealth)
    {
        healthBar.fillAmount = ((float) health) / ((float) maxHealth);

        if(healthAmount != null)
        {
            healthAmount.text = health + "/" + maxHealth;
        }
    }

    private void OnDestroy()
    {
        if(character != null)
        {
            character.Health.OnValueChanged -= OnUpdateHealth;
            character.MaxHealth.OnValueChanged -= OnUpdateMaxHealth;
        }
    }
}
