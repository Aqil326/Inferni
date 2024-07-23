public class SendCardToAlliesEffect : CardEffect
{
    public SendCardToAlliesEffect(SendCardToAlliesEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target == null || target is not Card cardInHand) return;

        var card = new Card(cardInHand.Data);
        foreach (var character in characterManager.GetAllAllies(cardOwner))
        {
            if (character.HandSize < character.Hand.Length)
            {
                character.AddCardToHandWithPosition(card, cardOwner.transform.position);
            }
            else
            {
                character.AddCardToTopDeckWithPosition(card, cardOwner.transform.position);
            }   
        }
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if(targetable is not Card cardInHand) return base.GetCardEffectScore(targetable);
        return cardInHand.Data.BaseScore;
    }
}
