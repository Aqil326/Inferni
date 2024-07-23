
using UnityEngine;

[CreateAssetMenu(fileName = "GainLifeAfterDiscardPlayerCardCharmData", menuName = "Data/Charms/Gain Life After Discard Player Card")]
public class GainLifeAfterDiscardPlayerCardCharmData : CharmData
{
    public int LifeAmount;
    public int DiscardCardAmount;
    
    public override Charm GetCharm()
    {
        return new GainLifeAfterDiscardPlayerCardCharm(this);
    }
}




