
using UnityEngine;

[CreateAssetMenu(fileName = "AddMirrorEffectToSelfCharmData", menuName = "Data/Charms/Add Mirror Effect to Self")]
public class AddMirrorEffectToSelfCharmData : CharmData
{
    public AddMirrorModifierEffectData ModifierEffect;
    public int DamageAmount;

    public override Charm GetCharm()
    {
        return new AddMirrorEffectToSelfCharm(this);
    }
}




