using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Deck
{
    public int MaxDeckSize { get; private set; }
    public int DeckSize => cards.Count;

    private Stack<Card> cards = new Stack<Card>();

    public Deck(List<CardData> cardDatas, Character owner)
    {
        foreach(var data in cardDatas)
        {
            var card = new Card(data);
            card.SetOwner(owner);
            cards.Push(card);
        }
        Shuffle();
        MaxDeckSize = cardDatas.Count;
    }

    public Deck(string[] cardIds, Character owner)
    {
        for (int i = cardIds.Length - 1; i > 0; i--)
        {
            var cardData = GameDatabase.GetCardData(cardIds[i]);
            var card = new Card(cardData);
            card.SetOwner(owner);
            cards.Push(card);
        }
        MaxDeckSize = cardIds.Length;
    }

    public string[] GetDecklist()
    {
        var list = new string[cards.Count];

        for(int i = 0; i < list.Length; i++)
        {
            list[i] = cards.ElementAt(i).Data.InternalID;
        }

        return list;
    }

    public Card Draw()
    {
        return cards.Pop();
    }

    public Card DrawSpecificCard(string cardId)
    {
        var cardList = cards.ToList();

        Card card = null;
        foreach(var c in cardList)
        {
            if(c.Data.InternalID == cardId)
            {
                card = c;
            }
        }

        if (card == null)
        {
            Debug.LogError($"No card with Card Id {cardId} found in the deck. Returning new card");
            var newCard = new Card(GameDatabase.GetCardData(cardId));
            return newCard;
        }

        cardList.Remove(card);
        cardList.Shuffle();
        cards = new Stack<Card>(cardList);
        return card;
    }

    public void AddCardToTop(Card card)
    {
        cards.Push(card);
    }

    public void AddCards(List<Card> cardsToRefill, bool shuffle = false)
    {
        foreach(var c in cardsToRefill)
        {
            cards.Push(c);
        }

        if (shuffle)
        {
            Shuffle();
        }
    }

    public void Shuffle()
    {
        var list = cards.ToList();
        list.Shuffle();
        cards = new Stack<Card>(list);
    }
}


