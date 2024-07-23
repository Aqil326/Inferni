using UnityEngine;

[CreateAssetMenu(fileName = "ChangeCardCastTimeCharmData", menuName = "Data/Charms/Change Card Cast Time")]
public class ChangeCardCastTimeCharmData: CharmData
{
    public int CastTimeReduction;
    
    public override Charm GetCharm()
    {
        return new ChangeCardCastTimeCharm(this);
    }
}