using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ApplyAdjacsentCharmEffectCharm : Charm
{
    private Dictionary<int, Charm> attachedCharms = new Dictionary<int, Charm>();
    public ApplyAdjacsentCharmEffectCharm(ApplyAdjacsentCharmEffectCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        AttachAdjacsentCharmEffects();
    }

    private void AttachAdjacsentCharmEffects()
    {
        var charmSlotNumber = GlobalGameSettings.Settings.CharmSlotsNumber;
        if (CharmSlot == null || CharmSlot.Index < 0 || CharmSlot.Index > charmSlotNumber - 1) return;
        
        var currentSlotIndex = CharmSlot.Index;

        var targetIndexes = new List<int>() { currentSlotIndex - 1, currentSlotIndex + 1 };
        var adjacsentIndexes = targetIndexes.Where(index => index > -1 && index < charmSlotNumber);
        
        foreach (var index in adjacsentIndexes)
        {
            var charmSlot = character.CharmSlots[index];
            charmSlot.CharmAddedEvent += OnAdjacsentCharmAdded;
            charmSlot.CharmRemovedEvent += OnAdjacsentCharmRemoved;
            if (!charmSlot.IsEmpty)
            {
                AttachAdjacsentCharmEffect(charmSlot);    
            }
        }
    }
    
    private void AttachAdjacsentCharmEffect(CharmSlot charmSlot)
    {
        var charmCopy = charmSlot.Charm.Clone();
        charmCopy.AttachToCharacter(character);
        attachedCharms.Add(charmSlot.Index, charmCopy);
    }

    private void OnAdjacsentCharmAdded(CharmSlot charmSlot)
    {
        AttachAdjacsentCharmEffect(charmSlot);
    }
    
    private void OnAdjacsentCharmRemoved(CharmSlot charmSlot)
    {
        var attachedCharm = attachedCharms[charmSlot.Index];
        if (attachedCharm == null) return;
        
        attachedCharm.RemoveCharm();
        attachedCharms.Remove(charmSlot.Index);
    }
    
    private void DetachAdjacsentCharmEffects()
    {
        if (attachedCharms.Count <= 0) return;
        
        foreach (var attachedCharm in attachedCharms.Values)
        {
            attachedCharm.RemoveCharm();
        }
        attachedCharms = new Dictionary<int, Charm>();
    }
    
    public override void RemoveCharm()
    {
        base.RemoveCharm();
        DetachAdjacsentCharmEffects();
    }
}
