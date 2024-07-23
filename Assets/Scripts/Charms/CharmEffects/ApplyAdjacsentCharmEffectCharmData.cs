using UnityEngine;

[CreateAssetMenu(fileName = "ApplyAdjacsentCharmData", menuName = "Data/Charms/Apply Adjacsent Charm Effect")]
public class ApplyAdjacsentCharmEffectCharmData: CharmData
{
    public override Charm GetCharm()
    {
        return new ApplyAdjacsentCharmEffectCharm(this);
    }
}