using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct ColorForCardColor
{
    public CardColors CardColor;
    public Color Color;
}

[Serializable]
public struct ColorForEffect
{
    public CardEffectCategory EffectType;
    public Color BackgroundColor;
    public Color TextColor;
}

[Serializable]
public class ArenaDraftPackPoolList
{
    public List<DraftPackPool> ArenaDraftPackPools;
}

[Serializable]
public class DraftPackPool
{
    public List<CardData> cardPool;
    public List<CharmData> charmPool;
}

[Serializable]
public class BotSettings
{
    public int ReviveScoreBonus = 1000;
    public int UnplayableCardScore = -2000;
    public int LifeRemainingDamageBonus = 1;
    public int LifeRemainingHealBonus = 1;
    public int NonXDataCardScore = -100;
    public int KillBonus = 25;
    public int DownedBonus = 10;
    public int FullHealthHealDeduction = -50;
    [SerializeField]
    private List<CardCategoryPositiveness> cardCategoryPositivenesses;

    private Dictionary<CardEffectCategory, bool> cardCategoryPositivenessDictionary;

    public bool GetCardCategoryPositiveness(CardEffectCategory cardCategory)
    {
        if(cardCategoryPositivenessDictionary == null)
        {
            cardCategoryPositivenessDictionary = new Dictionary<CardEffectCategory, bool>();
            foreach(var c in cardCategoryPositivenesses)
            {
                cardCategoryPositivenessDictionary.Add(c.category, c.isPositive);
            }
        }

        if(cardCategoryPositivenessDictionary.TryGetValue(cardCategory, out bool isPositive))
        {
            return isPositive;
        }
        Debug.Log($"No Positiveness was setup for Category {cardCategory}");
        //Assume is positive by default
        return true;
    }

    [Serializable]
    class CardCategoryPositiveness
    {
        public CardEffectCategory category;
        public bool isPositive;
    }
}

[CreateAssetMenu(fileName = "GlobalGameSettings", menuName = "Data/Global Game Settings")]
public class GlobalGameSettings : ScriptableObject
{
    private static GlobalGameSettings settings;

    public static GlobalGameSettings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = Resources.Load<GlobalGameSettings>("Settings/GlobalGameSettings");
            }
            return settings;
        }
    }

    public int MaxPlayerAmount = 8;
    public int MinPlayerAmount = 4;
    public float WaitForPlayersTimer = 5;
    public float DebugWaitForPlayersTimer = 3;

    public int TeamSize = 2;

    public int StartHandSize = 4;
    public int MaxHandSize = 5;
    public int HandSizeIncreasePerDraft = 1;
    public float DrawTimer = 5;
    public int DraftCardsPerPack = 10;
    public List<DraftPackPool> DraftPoolByRound;
    public List<ArenaDraftPackPoolList> DraftPoolByArenaAndRound;

    public float[] EnergyGenerationTimes;
    public int[] MaxEnergyAmounts;
    public int MaxEnergyPoolLevel;
    public float[] EnergyUpgradeTimes;

    public int CharmSlotsNumber = 4;

    public List<Sprite> TeamSprites;
    public List<Color> TeamColors;
    public List<string> TeamNames;

    public List<ColorForCardColor> CardColors;
    public List<ColorForEffect> EffectColors;
    public List<TargetIcons> TargetIcons;

    private Dictionary<CardColors, Color> cardColorsDictionary;
    private Dictionary<CardEffectCategory, ColorForEffect> effectColorsDictionary;
    private Dictionary<CardTarget, TargetIcons> targetIconsDictionary;

    public float TravelSpeed;
    public float CardProjectileTravelSpeed = 1f;
    public List<TravelSpeedData> TravelSpeeds;
    public List<TravelSpeedData> CardTravelSpeeds;
    public float SendCardCastingDuration = 3f;

    [Range(0, 1)]
    public float ChanceForDraftPacktoHaveCharm = 0.5f;

    public int MaxNumberOfCharmsPerDraftPack = 1;

    public float DraftPackTravelDuration = 1f;

    public int DeathTickIncreaseDuration = 3;
    public int DraftToCombatStartDelay = 3;
    public int DeathTickDamageAmount = 1;
    public float DeathTickIncreasePerTick = 0.25f;

    public BotSettings BotSettings;
    public int CardDrawOrDiscardScore = 5;
    public int NemesisDefeatRPGain;
    public string NemesisTitle;
    public string NemesisDescription;
    public int TrainingModePlayersCount = 2;
    public float GameplayCameraFarPlane = 1000;

    [Serializable]
    public struct TravelSpeedData
    {
        public int ArenaGroup;
        public float Speed;
    }

    public float GetProjectileSpeed(int arenaGroup)
    {
        return GameManager.Instance.DisableRP ? TravelSpeed : GetTravelSpeed(TravelSpeeds, arenaGroup);
    }
    
    public float GetCardProjectileSpeed(int arenaGroup)
    {
        return GameManager.Instance.DisableRP ? CardProjectileTravelSpeed : GetTravelSpeed(CardTravelSpeeds, arenaGroup);
    }

    private float GetTravelSpeed(List<TravelSpeedData> travelSpeeds, int arenaGroup)
    {
        var index = travelSpeeds.FindIndex(item => item.ArenaGroup == arenaGroup);
        if (index < 0)
        {
            index = 0;
        }
        return travelSpeeds.ElementAt(index).Speed;
    }

    public List<DraftPackPool> GetPackPoolsForRound(int draftRound)
    {
        var targetList = draftRound < Settings.DraftPoolByArenaAndRound.Count ?
            Settings.DraftPoolByArenaAndRound[draftRound] : Settings.DraftPoolByArenaAndRound[^1];
        return targetList.ArenaDraftPackPools;
    }

    public DraftPackPool GetPackPoolForRound(int draftRound)
    {
        return draftRound < Settings.DraftPoolByRound.Count ?
            Settings.DraftPoolByRound[draftRound] : Settings.DraftPoolByRound[^1];
    }
    
    public Color GetCardColor(CardColors color)
    {
        if (cardColorsDictionary == null)
        {
            cardColorsDictionary = new Dictionary<CardColors, Color>();
            foreach (var c in CardColors)
            {
                cardColorsDictionary.Add(c.CardColor, c.Color);
            }
        }

        return cardColorsDictionary[color];
    }

    public Sprite GetTeamSprite(int teamIndex)
    {
        if(teamIndex < TeamSprites.Count)
        {
            return TeamSprites[teamIndex];
        }
        Debug.LogError($"No Sprite was set for Team Index {teamIndex}");
        return null;
    }

    public Color GetTeamColor(int teamIndex)
    {
        if (teamIndex < TeamColors.Count)
        {
            return TeamColors[teamIndex];
        }
        Debug.LogError($"No Color was set for Team Index {teamIndex}");
        return Color.gray;
    }

    public string GetTeamName(int teamIndex)
    {
        if (teamIndex < TeamNames.Count)
        {
            return TeamNames[teamIndex];
        }
        Debug.LogError($"No Name was set for Team Index {teamIndex}");
        return "Wayward Souls";
    }

    public ColorForEffect GetEffectColors(CardEffectCategory effect)
    {
        if (effectColorsDictionary == null)
        {
            effectColorsDictionary = new Dictionary<CardEffectCategory, ColorForEffect>();
            foreach (var eff in EffectColors)
            {
                effectColorsDictionary.Add(eff.EffectType, eff);
            }
        }

        if(effectColorsDictionary.TryGetValue(effect, out ColorForEffect color))
        {
            return color;
        }
        Debug.LogError($"Couldn't find color for Card Effect {effect}");

        return effectColorsDictionary[CardEffectCategory.Other];
    }

    public TargetIcons GetTargetIcon(CardTarget target)
    {
        if (targetIconsDictionary == null)
        {
            targetIconsDictionary = new Dictionary<CardTarget, TargetIcons>();
            foreach (var icon in TargetIcons)
            {
                targetIconsDictionary.Add(icon.target, icon);
            }
        }
        if(!targetIconsDictionary.TryGetValue(target, out TargetIcons targetIcon))
        {
            Debug.LogError($"Target Icon wasn't set for target {target}");
            return null; 
        }
        return targetIcon;
    }

    
}

