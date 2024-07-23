
using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseMaxLifeCharmData", menuName = "Data/Charms/Increase Max Life")]
public class IncreaseMaxLifeCharmData : CharmData
{
    public int LifeAmount;
    public override Charm GetCharm()
    {
        return new IncreaseMaxLifeCharm(this);
    }
}




