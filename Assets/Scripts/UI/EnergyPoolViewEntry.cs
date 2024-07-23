using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnergyPoolViewEntry : MonoBehaviour
{
    [SerializeField]
    private Image background;

    [SerializeField]
    private TextMeshProUGUI energyAmount;

    private EnergyPool pool;

    public void SetPool(EnergyPool pool)
    {
        if(this.pool != null)
        {
            this.pool.Level.OnValueChanged -= ShowView;
            this.pool.Energy.OnValueChanged -= UpdateEnergy;
        }
        this.pool = pool;
        this.pool.Level.OnValueChanged += ShowView;
        this.pool.Energy.OnValueChanged += UpdateEnergy;
        gameObject.SetActive(false);
    }

    private void ShowView(int previous, int current)
    {
        if(pool.IsEmpty)
        {
            return;
        }

        gameObject.SetActive(true);
        background.color = GlobalGameSettings.Settings.GetCardColor(pool.Color.Value);
        UpdateEnergy(0, pool.Energy.Value);
    }

    private void UpdateEnergy(int previous, int current)
    {
        energyAmount.text = current.ToString()
;
    }
}
