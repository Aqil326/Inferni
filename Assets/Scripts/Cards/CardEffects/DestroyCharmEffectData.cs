using UnityEngine;

[CreateAssetMenu(fileName = "DestroyCharmEffectData", menuName = "Data/Card Effects/Destroy Charm")]
public class DestroyCharmEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new DestroyCharmEffect(this);
    }
}
