
using UnityEngine;

[CreateAssetMenu(fileName = "DrawCardAfterKillPlayerCharmData", menuName = "Data/Charms/Draw Card After Kill Player")]
public class DrawCardAfterKillPlayerCharmData : CharmData
{
    public override Charm GetCharm()
    {
        return new DrawCardAfterKillPlayerCharm(this);
    }
}




