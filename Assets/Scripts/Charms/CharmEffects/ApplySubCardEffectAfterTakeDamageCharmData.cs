
using UnityEngine;

[CreateAssetMenu(fileName = "ApplySubCardEffectAfterTakeDamageCharmData", menuName = "Data/Charms/Apply SubCard Effect After Take Damage")]
public class ApplySubCardEffectAfterTakeDamageCharmData : CharmData
{
    public CardData CardData;
    public CardTarget Tagret;

    public override Charm GetCharm()
    {
        return new ApplySubCardEffectAfterTakeDamageCharm(this);
    }
}




