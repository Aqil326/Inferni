using System;
using UnityEngine;

public class CharmSlot : ITargetable
{
    public Charm Charm { get; private set; }
    public int Index { get; private set; }
    public Character Owner => character;

    public bool IsEmpty => Charm == null;

    string ITargetable.TargetId => $"{character.PlayerData.Id}#CharmSlot{Index}";
    Vector3 ITargetable.Position => ((ITargetable)character).Position;

    public event Action<CharmSlot> CharmAddedEvent;
    public event Action<CharmSlot> CharmRemovedEvent;

    private Character character;
    
    public CharmSlot(int index, Character character)
    {
        Index = index;
        this.character = character;
    }

    public void AddCharm(CharmData charmData)
    {
        if(!IsEmpty)
        {
            RemoveCharm();
        }
        Charm = charmData.GetCharm();
        Charm.CharmSlot = this;
        if (character.IsServer)
        {
            Charm.AttachToCharacter(character);
        }
        CharmAddedEvent?.Invoke(this);

        Charm.CharmRemovedEvent += OnCharmRemoved;
    }

    private void OnCharmRemoved(Charm charm)
    {
        charm.CharmRemovedEvent -= OnCharmRemoved;
        CharmRemovedEvent?.Invoke(this);
    }

    public void RemoveCharm()
    {
        if(Charm != null)
        {
            var charm = Charm;
            Charm = null;
            charm.RemoveCharm();
        }
    }

    bool ITargetable.ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        return false;
    }
}
