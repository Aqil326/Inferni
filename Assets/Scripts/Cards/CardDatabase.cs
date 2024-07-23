using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameDatabase
{
    private static Dictionary<string, CardData> cards = new Dictionary<string, CardData>();
    private static Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>();
    private static Dictionary<string, CharmData> charms = new Dictionary<string, CharmData>();
    private static Dictionary<string, AddModifierEffectData> modifiers = new Dictionary<string, AddModifierEffectData>();

    public static List<CardData> Cards => cards.Values.ToList<CardData>();
    public static List<CharmData> Charms => charms.Values.ToList<CharmData>();

    static GameDatabase()
    {
        var cardDatas = Resources.LoadAll<CardData>("Cards");

        foreach(var c in cardDatas)
        {
            if(cards.ContainsKey(c.InternalID))
            {
                Debug.LogError($"Card {c} has the same id {c.InternalID} as {cards[c.InternalID]}'s id {cards[c.InternalID].InternalID}");
                continue;
            }
            cards.Add(c.InternalID, c);
        }

        var characterDatas = Resources.LoadAll<CharacterData>("Characters");

        foreach (var c in characterDatas)
        {
            characters.Add(c.InternalID, c);
        }

        var charmDatas = Resources.LoadAll<CharmData>("Charms");

        foreach(var c in charmDatas)
        {
            charms.Add(c.InternalID, c);
        }

        var modifierDatas = Resources.LoadAll<AddModifierEffectData>("Cards/Effects");

        foreach (var c in modifierDatas)
        {
            modifiers.Add(c.InternalID, c);
        }

        Resources.UnloadUnusedAssets();
    }

    public static CharacterData InitialCharacter
    {
        get
        {
            return characters.Values.First();
        }
    }

    public static bool CharacterExists(string characterId)
    {
        return characters.ContainsKey(characterId);
    }

    public static CharacterData GetCharacterData(string characterId)
    {
        if(characters.TryGetValue(characterId, out CharacterData data))
        {
            return data;
        }

        Debug.LogError($"Couldn't find any Character with character Id of {characterId}");
        return null;
    }

    // TODO: Hack
    public static CharacterData GetCharacterDatabyName(string characterName)
    {
        var myValue = characters.FirstOrDefault(x => x.Value.Name == characterName).Value;
        return myValue;
    }

    public static List<CharacterData> GetAllCharacters()
    {
        return characters.Values.ToList();
    }

    public static CardData GetCardData(string cardId)
    {
        if(cards.TryGetValue(cardId, out CardData data))
        {
            return data;
        }
        Debug.LogError($"Couldn't find any Card with card ID of {cardId}");
        return null;
    }

    public static CharmData GetCharmData(string charmId)
    {
        if (charms.TryGetValue(charmId, out CharmData data))
        {
            return data;
        }
        Debug.LogError($"Couldn't find any Charm with ID of {charmId}");
        return null;
    }

    public static AddModifierEffectData GetModifierData(string modifierId)
    {
        if (modifiers.TryGetValue(modifierId, out AddModifierEffectData modifier))
        {
            return modifier;
        }
        Debug.LogError($"Couldn't find any Modifier with modifier ID of {modifierId}");
        return null;
    }
}
