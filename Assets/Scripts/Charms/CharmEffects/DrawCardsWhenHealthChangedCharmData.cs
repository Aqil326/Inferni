
using UnityEngine;

[CreateAssetMenu(fileName = "DrawCardWhenHealthChangedCharmData", menuName = "Data/Charms/Draw Cards When Health Changed")]
public class DrawCardsWhenHealthChangedCharmData : CharmData
{
    public int cardsDrawn;
    public int healthChange;

    public override Charm GetCharm()
    {
        return new DrawCardsWhenHealthChangedCharm(this);
    }
}




