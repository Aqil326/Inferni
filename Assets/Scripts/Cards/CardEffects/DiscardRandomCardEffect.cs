using System.Collections.Generic;
using UnityEngine;

public class DiscardRandomCardEffect : CardEffect
{
    public DiscardRandomCardEffect(DiscardRandomCardEffectData data) : base(data)
    {

    }
    
    public override string GetEffectCardText()
    {
        var data = GetData<DiscardRandomCardEffectData>();
        int number = data.cardsAmount;
        string text = base.GetEffectCardText();
        return text.Replace("{X}", number.ToString());
    }

    private static List<int> PickNRandomDistinct(List<int> array, int pickCount)
    {
        if (array.Count == 0) return new List<int>();
        
        if (pickCount > array.Count) return array;

        var tempList = new List<int>(array);
        var resultList = new List<int>();

        for (var i = 0; i < pickCount; i++)
        {
            var randIndex = Random.Range(i, tempList.Count);
            (tempList[i], tempList[randIndex]) = (tempList[randIndex], tempList[i]);

            resultList.Add(tempList[i]);
        }

        return resultList;
    }
    
    protected override void InternalApplyEffect(ITargetable target)
    {
        if (target is not Character character) return;
        
        var cardIndexes = new List<int>();
        for(var i = 0; i < character.Hand.Length; i++)
        {
            if (character.Hand[i] != null)
            {
                cardIndexes.Add(i);
            }
        }

        var data = GetData<DiscardRandomCardEffectData>();
        var distinctIndexes = PickNRandomDistinct(cardIndexes, data.cardsAmount);
        if (distinctIndexes.Count <= 0) return;
        
        character.BatchDiscardCard(distinctIndexes, card);
    }

    public override int GetCardEffectScore(ITargetable targetable)
    {
        if (targetable is not Character character)  return base.GetCardEffectScore(targetable);

        int cardsAmount = GetData<DiscardRandomCardEffectData>().cardsAmount;

        if(character.HandSize < cardsAmount)
        {
            cardsAmount = character.HandSize;
        }
        return -cardsAmount * GlobalGameSettings.Settings.CardDrawOrDiscardScore;
    }
}
