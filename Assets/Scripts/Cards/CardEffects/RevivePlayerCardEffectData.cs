using UnityEngine;

[CreateAssetMenu(fileName = "RevivePlayerCardEffectData", menuName = "Data/Card Effects/Review Player")]
public class RevivePlayerCardEffectData : CardEffectData
{
    public int Health;
    
    public override CardEffect CreateEffect()
    {
        return new RevivePlayerCardEffect(this);
    }
}
