using UnityEngine;

[CreateAssetMenu(fileName = "CopyCharmEffectData", menuName = "Data/Card Effects/Copy Charm")]
public class CopyCharmEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new CopyCharmEffect(this);
    }
}
