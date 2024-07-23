using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BotProfile", menuName = "Data/Bot Profile")]
public class BotProfile : ScriptableObject
{
    [Serializable]
    class CardCategoryScore
    {
        public CardEffectCategory cardCategory;
        public int score;
    }
    [SerializeField]
    private List<CardCategoryScore> cardCategoryScores;

    [SerializeField]
    private int selfishnessBonus;


    [SerializeField]
    private int agressivenessBonus;

    public int SelfishnessBonus => selfishnessBonus;
    public int AgressivenessBonus => agressivenessBonus;

    private Dictionary<CardEffectCategory, int> cardCategoryScoreDictionary; 

    public Dictionary<CardEffectCategory, int> CardCategoryScores
    {
        get
        {
            if(cardCategoryScoreDictionary == null)
            {
                cardCategoryScoreDictionary = new Dictionary<CardEffectCategory, int>();
                foreach (var score in cardCategoryScores)
                {
                    cardCategoryScoreDictionary.Add(score.cardCategory, score.score);
                }
            }
            return cardCategoryScoreDictionary;
        }
    }
}


