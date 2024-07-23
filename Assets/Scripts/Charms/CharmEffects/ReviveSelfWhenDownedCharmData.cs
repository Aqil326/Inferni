
using UnityEngine;

[CreateAssetMenu(fileName = "ReviveSelfWhenDownedCharmData", menuName = "Data/Charms/Revive Self When Downed")]
public class ReviveSelfWhenDownedCharmData : CharmData
{
    public int health;
    public override Charm GetCharm()
    {
        return new ReviveSelfWhenDownedCharm(this);
    }
}




