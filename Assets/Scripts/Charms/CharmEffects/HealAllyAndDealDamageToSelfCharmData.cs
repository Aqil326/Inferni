
using UnityEngine;

[CreateAssetMenu(fileName = "HealAllyAndDealDamageToSelfCharmData", menuName = "Data/Charms/Heal Ally Deal Damage To Self")]
public class HealAllyAndDealDamageToSelfCharmData : CharmData
{
    public int HealAmount;
    public int DamageAmount;
    public int HealthDiff;
    public float Frequency;
    
    public override Charm GetCharm()
    {
        return new HealAllyAndDealDamageToSelfCharm(this);
    }
}




