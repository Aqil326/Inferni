using UnityEngine;

[CreateAssetMenu(fileName = "SendCardToAlliesEffectData", menuName = "Data/Card Effects/Send Card To Allies")]
public class SendCardToAlliesEffectData : CardEffectData
{
    public override CardEffect CreateEffect()
    {
        return new SendCardToAlliesEffect(this);
    }
}
