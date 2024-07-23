using UnityEngine;

public class GainLifeAfterDiscardPlayerCardCharm : Charm
{
    public GainLifeAfterDiscardPlayerCardCharm(GainLifeAfterDiscardPlayerCardCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        characterManager.CharacterCardBatchDiscardedEvent += OnCharacterCardBatchDiscarded;
    }

    private void OnCharacterCardBatchDiscarded(Character targetCharacter, Card sourceCard, int discardAmount)
    {
        var data = GetData<GainLifeAfterDiscardPlayerCardCharmData>();
        if (sourceCard == null)
        {
            Debug.Log("Card is null");
            return;
        }
        if (sourceCard.Owner != character || discardAmount < data.DiscardCardAmount) return;

        character.Heal(data.LifeAmount);
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        characterManager.CharacterCardBatchDiscardedEvent -= OnCharacterCardBatchDiscarded;
    }
}
