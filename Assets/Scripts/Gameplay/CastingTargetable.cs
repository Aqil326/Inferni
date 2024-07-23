using Unity.Netcode;
using UnityEngine;

public class CastingTargetable : MonoBehaviour, ITargetable
{
    public string TargetId => Owner.PlayerData.Id + "#CastingIcon"; 

    public Vector3 Position => transform.position;

    public Character Owner { get; private set; }

    public void Init(Character character)
    {
        this.Owner = character;
    }

    public bool ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        if(!Owner.IsCasting.Value)
        {
            canBeTargeted = false;
            return true;
        }

        return false;
    }
}
