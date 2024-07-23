
using UnityEngine;

[CreateAssetMenu(fileName = "AddExtraEffectToProjectilesCharmData", menuName = "Data/Charms/Add Extra Effect to Projectile")]
public class AddExtraEffectToProjectilesCharmData : CharmData
{
    public CardEffectData cardEffectData;
    public CardEffectCategory requestedEffectType;

    public override Charm GetCharm()
    {
        return new AddExtraEffectToProjectilesCharm(this);
    }
}




