using UnityEngine;

[CreateAssetMenu(fileName = "PlayTargetCardOnSelfForFreeEffectData", menuName = "Data/Card Effects/Play Target Card On Self For Free Effect")]
public class PlayTargetCardOnSelfForFreeEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new PlayTargetCardOnSelfForFreeEffect(this);
    }
}
