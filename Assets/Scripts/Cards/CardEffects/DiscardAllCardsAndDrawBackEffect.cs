using System.Collections.Generic;
using UnityEngine.TextCore.Text;

public class DiscardAllCardsAndDrawBackEffect : CardEffect
{
    public DiscardAllCardsAndDrawBackEffect(DiscardAllCardsAndDrawBackEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Character character) return;
        
        var discardIndexes = new List<int>();
        for (var i = 0; i < character.Hand.Length; i++)
        {
            var card = character.Hand[i];
            if (card != null)
            {
                discardIndexes.Add(i);
            }
        }

        if (discardIndexes.Count == 0) return;
            
        character.BatchDiscardCard(discardIndexes, card);
        character.BatchDrawCard(discardIndexes.Count);
    }
}
