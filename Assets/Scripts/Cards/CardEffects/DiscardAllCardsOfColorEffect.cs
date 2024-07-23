using System.Collections.Generic;

public class DiscardAllCardsOfColorEffect : CardEffect
{
    public DiscardAllCardsOfColorEffect(DiscardAllCardsOfColorEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Character character) return;
        
        var data = GetData<DiscardAllCardsOfColorEffectData>();
        var discardIndexes = new List<int>();
        for (var i = 0; i < character.Hand.Length; i++)
        {
            var handCard = character.Hand[i];
            if (handCard != null && handCard.Data.CardColor == data.Color)
            {
                discardIndexes.Add(i);
            }
        }

        if (discardIndexes.Count == 0) return;
        
        character.BatchDiscardCard(discardIndexes, card);
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if (targetable is not Character character) return base.GetCardEffectScore(targetable);

        var data = GetData<DiscardAllCardsOfColorEffectData>();
        var discardIndexes = new List<int>();
        for (var i = 0; i < character.Hand.Length; i++)
        {
            var handCard = character.Hand[i];
            if (handCard != null && handCard.Data.CardColor == data.Color)
            {
                discardIndexes.Add(i);
            }
        }
        return -discardIndexes.Count * GlobalGameSettings.Settings.CardDrawOrDiscardScore;
    }
}
