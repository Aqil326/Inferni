using UnityEngine;

[CreateAssetMenu(fileName = "SwitchPlayersEffectData", menuName = "Data/Card Effects/Switch Players")]
public class SwitchPlayersEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new SwitchPlayersEffect(this);
    }
}
