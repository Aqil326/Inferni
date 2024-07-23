using UnityEngine;

[CreateAssetMenu(fileName = "SetHealthEffectData", menuName = "Data/Card Effects/Set Health")]
public class SetHealthEffectData : CardEffectData
{
    public int health;

    public override CardEffect CreateEffect()
    {
        return new SetHealthEffect(this);
    }
}


