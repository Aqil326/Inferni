public class DrawCardsAfterDiscardCardCharm : Charm
{
    public DrawCardsAfterDiscardCardCharm(DrawCardsAfterDiscardCardCharmData data) : base(data)
    {

    }

    public override void AttachToCharacter(Character character)
    {
        base.AttachToCharacter(character);
        characterManager.CharacterCardBatchDiscardedEvent += OnCharacterCardBatchDiscarded;
    }

    private void OnCharacterCardBatchDiscarded(Character targetCharacter, Card sourceCard, int discardAmount)
    {
        var data = GetData<DrawCardsAfterDiscardCardCharmData>();
        if (targetCharacter != character || discardAmount < data.DiscardCardAmount) return;
        character.BatchDrawCard(data.DrawCardAmount);
    }

    public override void RemoveCharm()
    {
        base.RemoveCharm();
        if (character != null)
        {
            characterManager.CharacterCardBatchDiscardedEvent -= OnCharacterCardBatchDiscarded;
        }
    }
}
