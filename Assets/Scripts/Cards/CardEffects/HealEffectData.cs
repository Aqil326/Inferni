using UnityEngine;

[CreateAssetMenu(fileName = "HealEffectData", menuName = "Data/Card Effects/Heal")]
public class HealEffectData : CardEffectData
{
    public int healAmount; 

    public override CardEffect CreateEffect()
    {
        return new HealEffect(this);
    }
}
