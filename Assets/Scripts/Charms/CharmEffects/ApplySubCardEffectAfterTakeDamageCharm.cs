using System;
using System.Linq;
using UnityEngine;

public class ApplySubCardEffectAfterTakeDamageCharm : Charm
{
    public ApplySubCardEffectAfterTakeDamageCharm(ApplySubCardEffectAfterTakeDamageCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        character.Health.OnValueChanged += OnCharacterHealthChange;
    }
    
    private void OnCharacterHealthChange(int oldHealth, int newHealth)
    {
        if (oldHealth <= newHealth) return;
        
        var data = GetData<ApplySubCardEffectAfterTakeDamageCharmData>();
        switch (data.Tagret)
        {
            case CardTarget.AllOpponents:
            {
                var opponents = characterManager.GetAllOpponents(character);
                var aliveOpponents = opponents.Where(opponent => !opponent.IsDead).ToList();
                if (aliveOpponents.Count == 0) return;
        
                foreach (var opponent in aliveOpponents)
                {
                    character.PlayCard(data.CardData, opponent, true);
                }
                character.RemoveCharm(CharmData);        
                break;
            }
            default:
                // Extend to support other card target types
                return;
        }
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        if (character != null)
        {
            character.Health.OnValueChanged -= OnCharacterHealthChange;
        }
    }
}
