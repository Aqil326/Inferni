
using UnityEngine;

[CreateAssetMenu(fileName = "GainLifeAfterTakeDamageCharmData", menuName = "Data/Charms/Gain Life After Take Damage")]
public class GainLifeAfterTakeDamageCharmData : CharmData
{
    public int HealAmount;
    public float HealDelay;
    public override Charm GetCharm()
    {
        return new GainLifeAfterTakeDamageCharm(this);
    }
}




