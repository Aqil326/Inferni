
using UnityEngine;

[CreateAssetMenu(fileName = "GainLifeAfterDealDamageCharmData", menuName = "Data/Charms/Gain Life After Deal Damage")]
public class GainLifeAfterDealDamageCharmData : CharmData
{
    public float percentage;
    public override Charm GetCharm()
    {
        return new GainLifeAfterDealDamageCharm(this);
    }
}




