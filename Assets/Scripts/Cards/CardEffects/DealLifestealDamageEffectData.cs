using UnityEngine;

[CreateAssetMenu(fileName = "DealLifestealDamageEffectData", menuName = "Data/Card Effects/Deal Lifesteal Damage")]
public class DealLifestealDamageEffectData : CardEffectData
{
    public int damageAmount;

    public override CardEffect CreateEffect()
    {
        return new DealLifestealDamageEffect(this);
    }
}
