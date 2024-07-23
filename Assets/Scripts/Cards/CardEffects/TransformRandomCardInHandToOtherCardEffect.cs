using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformRandomCardInHandToOtherCardEffect : CardEffect
{
    public TransformRandomCardInHandToOtherCardEffect(TransformRandomCardInHandToOtherCardEffectData data) : base(data)
    {

    }

    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is Character character)
        {
            var data = GetData<TransformRandomCardInHandToOtherCardEffectData>();
            List<int> cardIndexes = new List<int>();

            var targetCharacter = character;
            for(int i = 0; i < targetCharacter.Hand.Length; i++)
            {
                if (targetCharacter.Hand[i] != null && targetCharacter.Hand[i].Data != data.cardData)
                {
                    cardIndexes.Add(i);
                }
            }

            if (cardIndexes.Count > 0)
            {
                int indexToTransform = cardIndexes[Random.Range(0, cardIndexes.Count)];
                var cardId = data.cardData.InternalID;
                targetCharacter.TransformCardInHand(indexToTransform, cardId);
            }
        }
    }

    public override bool HasSubCard(out CardData subCard)
    {
        subCard = GetData<TransformRandomCardInHandToOtherCardEffectData>().cardData;
        return true;
    }

    public override int GetCardEffectScore(ITargetable target)
    {
        return GetData<TransformRandomCardInHandToOtherCardEffectData>().cardData.BaseScore;

    }
}