using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterPosition
{
    None,
    Leader,
    Lowest
}

[Serializable]
public class CharacterPositionBonus
{
    public CharacterPosition Position;
    public int BonusAdded = 0;
    public float BonusMultiplier = 1f;

    public float GetValue(float originalValue, Character character)
    {
        var characterManager = GameManager.GetManager<CharacterManager>();
        float finalValue = originalValue * BonusMultiplier + BonusAdded;
        switch (Position)
        {
            case CharacterPosition.Leader:
                if(character == characterManager.HighestCharacter)
                {
                    return finalValue;
                }
                break;
            case CharacterPosition.Lowest:
                if (character == characterManager.LowestCharacter)
                {
                    return finalValue;
                }
                break;
            case CharacterPosition.None:
                return finalValue;

        }
        return originalValue;
    }

    public int GetValue(int originalValue, Character character)
    {
        return Mathf.FloorToInt(GetValue(((float)originalValue), character));
    }
}

[CreateAssetMenu(fileName = "DealDamageEffectData", menuName = "Data/Card Effects/Deal Damage")]
public class DealDamageEffectData : CardEffectData
{
    public int DelayTime = 1;
    public int damageAmount;
    public bool useProjectileLifetimeMultiplier;
    public List<CharacterPositionBonus> characterPositionBonuses;

    public override CardEffect CreateEffect()
    {
        return new DealDamageEffect(this);
    }
}