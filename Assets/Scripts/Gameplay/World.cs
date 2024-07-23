using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class World : NetworkBehaviour, ITargetable 
{
    public const string WORLD_TARGET_ID = "World";

    [SerializeField]
    private WorldView worldView;

    Vector3 ITargetable.Position => transform.position;
    string ITargetable.TargetId => WORLD_TARGET_ID;

    private GlobalModifier modifier;
    private NetworkVariable<float> RemainingModifierDuration = new NetworkVariable<float>();


    private void InitView()
    {
        worldView.Init(this);
    }


    public void AddModifier(GlobalModifier modifier)
    {
        if(this.modifier != null)
        {
            this.modifier.RemoveModifier();
        }

        this.modifier = modifier;
        modifier.AttachToWorld(this);
        modifier.ModifierRemoveEvent += OnModifierRemove;
        modifier.TimerUpdatedEvent += OnModifierTimerUpdated;
        AddModifierClientRPC(modifier.ID);
    }

    [ClientRpc]
    private void AddModifierClientRPC(string modifierId)
    {
        var modifierData = GameDatabase.GetModifierData(modifierId);
        var modifier = (GlobalModifier) modifierData.GetModifier();
        worldView.AddModifier(modifier, RemainingModifierDuration);
    }

    private void OnModifierTimerUpdated()
    {
        RemainingModifierDuration.Value = modifier.Timer;
    }

    private void OnModifierRemove(Modifier modifier)
    {
        modifier.ModifierRemoveEvent -= OnModifierRemove;
        modifier.TimerUpdatedEvent -= OnModifierTimerUpdated;
        RemoveModifierClientRPC(modifier.ID);
        this.modifier = null;
    }

    [ClientRpc]
    private void RemoveModifierClientRPC(string modifierId)
    {
        var modifierData = GameDatabase.GetModifierData(modifierId);
        var modifier = (GlobalModifier) modifierData.GetModifier();
        worldView.RemoveModifier(modifier);
    }

    private void OnEnable()
    {
        EventBus.StartListening(EventBusEnum.EventName.START_ROUND_COMBAT_SERVER, StartCombatRound);
        EventBus.StartListening(EventBusEnum.EventName.END_ROUND_COMBAT_SERVER, EndCombatRound);
        EventBus.StartListening(EventBusEnum.EventName.CHARACTERS_INITIALIZED_CLIENT, InitView);
    }

    private void OnDisable()
    {
        EventBus.StopListening(EventBusEnum.EventName.START_ROUND_COMBAT_SERVER, StartCombatRound);
        EventBus.StopListening(EventBusEnum.EventName.END_ROUND_COMBAT_SERVER, EndCombatRound);
        EventBus.StopListening(EventBusEnum.EventName.CHARACTERS_INITIALIZED_CLIENT, InitView);
    }

    private void StartCombatRound()
    { 
        if(modifier != null)
        {
            modifier.Unpause();
        }
    }

    private void EndCombatRound()
    {
        if (modifier != null)
        {
            modifier.Pause();
        }
    }
    
    bool ITargetable.ShouldOverrideTargetAvailability(ref bool canBeTargeted, Card card)
    {
        return false;
    }

}
