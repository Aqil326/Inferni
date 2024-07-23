using UnityEngine;

[CreateAssetMenu(fileName = "InstaKillEffectData", menuName = "Data/Card Effects/Insta Kill")]
public class InstaKillEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new InstaKillEffect(this);
    }
}


