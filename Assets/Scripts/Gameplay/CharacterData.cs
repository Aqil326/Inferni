using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Data/Character")]
public class CharacterData : ScriptableObjectWithId
{
    public string Name;
    [TextArea]
    public string Description;
    public Sprite Sprite;
    public GameObject CharacterModel;
    public int MaxHealth;
    public int MaxDeathTimer;
    public float CardDrawTimer;
    public CardData[] StartingCards;
    public List<CharmDataWithIndex> StartingCharms;
}

[Serializable]
public struct CharmDataWithIndex
{
    public int Index;
    public CharmData CharmData;
}

