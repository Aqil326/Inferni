using System;
using UnityEngine;

public class GainLifeAfterDealDamageCharm : Charm
{
    public GainLifeAfterDealDamageCharm(GainLifeAfterDealDamageCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        characterManager.CharacterHealthChangedByCardEvent += OnCharacterHealthChangeByCard;
    }

    private void OnCharacterHealthChangeByCard(Character targetCharacter, int healthChange, Card card)
    {
        if (healthChange >= 0 || targetCharacter == null || card == null || card.Owner !=  character) return;
        
        var data = GetData<GainLifeAfterDealDamageCharmData>();
        var damage = Mathf.Abs(healthChange);
        var gainLife = (int)Math.Floor(damage * data.percentage);
        character.Heal(gainLife);
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        characterManager.CharacterHealthChangedByCardEvent -= OnCharacterHealthChangeByCard;
    }
}
