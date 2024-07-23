using UnityEngine;


[CreateAssetMenu(fileName = "DiscardTargetCardAndDrawUpToThreeCardsEffectData", menuName = "Data/Card Effects/Discard Card And Draw Up To Three Cards")]
public class DiscardTargetCardAndDrawCardsEffectData : CardEffectData
{
    public float Slowdown = 2;
    public int cardDrawAmount = 3;
    
    public override CardEffect CreateEffect()
    {
        return new DiscardTargetCardAndDrawCardsEffect(this);
    }
}
