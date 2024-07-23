
using UnityEngine;

[CreateAssetMenu(fileName = "DrawCardAfterDiscardCardCharmData", menuName = "Data/Charms/Draw Cards After Discard Card")]
public class DrawCardsAfterDiscardCardCharmData : CharmData
{
    public int DrawCardAmount;
    public int DiscardCardAmount;
    public override Charm GetCharm()
    {
        return new DrawCardsAfterDiscardCardCharm(this);
    }
}




