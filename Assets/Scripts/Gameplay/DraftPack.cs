using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public struct DraftPackData : INetworkSerializable
{
    public string ID;
    public string[] CardList;
    public string[] CharmList;
    public int CardCount;
    public int CharmCount;
    public ulong PreviousOwnerId;
    public ulong OwnerId;
    public int LastPickedIndex;
    public Vector3 EndPosition;

    public DraftPackData(string id, string[] cardList, string[] charmList, ulong previousOwnerId, ulong ownerId, Vector3 endPosition, int lastPickedIndex)
    {
        ID = id;
        CardList = cardList;
        CharmList = charmList;
        CardCount = cardList.Length;
        CharmCount = charmList.Length;
        OwnerId = ownerId;
        PreviousOwnerId = previousOwnerId;
        EndPosition = endPosition;
        LastPickedIndex = lastPickedIndex;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ID);
        serializer.SerializeValue(ref CardCount);
        serializer.SerializeValue(ref CharmCount);
        serializer.SerializeValue(ref PreviousOwnerId);
        serializer.SerializeValue(ref OwnerId);
        serializer.SerializeValue(ref EndPosition);
        serializer.SerializeValue(ref LastPickedIndex);
        if (serializer.IsReader)
        {
            CardList = new string[CardCount];
            CharmList = new string[CharmCount];
        }
        
        for (int n = 0; n < CardCount; n++)
        {
            serializer.SerializeValue(ref CardList[n]);
        }
        
        for(int n = 0; n < CharmCount; n++)
        {
            serializer.SerializeValue(ref CharmList[n]);
        }
    }
}

public class DraftPack
{
    public List<Card> Cards;
    public List<Charm> Charms;
    public Character PreviousOwner;
    public Character CurrentOwner;
    public bool IsComplete => Cards.Count == 0 && Charms.Count == 0;
    public string ID { get; private set; }
    public Action<DraftPack> DraftPackPassedEvent;
    public Action<DraftPack> DraftPackCompleteEvent;

    private int lastPickedIndex;
    
    public DraftPack(Character newCurrentOwner, List<Card> cards, List<Charm> charms)
    {
        CurrentOwner = newCurrentOwner;
        ID = Guid.NewGuid().ToString();
        Cards = cards;
        Charms = charms;
    }
    
    public DraftPackData GetData(Vector3 endPosition)
    {
        var cardList = new string[Cards.Count];
        for (int i = 0; i < Cards.Count; i++)
        {
            cardList[i] = Cards[i].Data.InternalID;
        }
        var charmList = new string[Charms.Count];
        for (int i = 0; i < Charms.Count; i++)
        {
            charmList[i] = Charms[i].CharmData.InternalID;
        }
        
        ulong previousOwnerId = (PreviousOwner != null) ? PreviousOwner.PlayerData.Id : 99;
        var data = new DraftPackData(ID, cardList, charmList, previousOwnerId, CurrentOwner.PlayerData.Id, endPosition, lastPickedIndex);        
        return data;
    }
    
    public void PassPack(Character newOwner)
    {
        PreviousOwner = CurrentOwner;
        CurrentOwner = newOwner;
        newOwner.ReceiveDraftPack(this);
        DraftPackPassedEvent?.Invoke(this);
    }
    
    public string GetFirstItemId()
    {
        if (IsComplete)
        {
            return string.Empty;
        }
        return Cards.Count > 0 ? Cards[0].Data.InternalID : Charms[0].CharmData.InternalID;
    }

    public void Pick(string pickedId, out Card card, out Charm charm)
    {
        card = null;
        charm = null;

        for (int i = 0; i < Cards.Count; i++)
        {
            Card c = Cards[i];
            if (c.Data.InternalID == pickedId)
            {
                card = c;
                lastPickedIndex = i;
                break;
            }
        }
        if (card != null)
        {
            Cards.Remove(card);
        }
        else
        {
            for (int i = 0; i < Charms.Count; i++)
            {
                Charm c = Charms[i];
                if (c.CharmData.InternalID == pickedId)
                {
                    lastPickedIndex = Cards.Count + i;
                    charm = c;
                    break;
                }
            }
            if (charm != null)
            {
                Charms.Remove(charm);
            }
            else
            {
                Debug.LogError($"Couldn't find a card or a charm with ID {pickedId} for {CurrentOwner.PlayerData.DisplayName}. Picking first item to unblock");
                if(Cards.Count > 0)
                {
                    card = Cards[0];
                    Cards.RemoveAt(0);
                }
                else if(Charms.Count > 0)
                {
                    charm = Charms[0];
                    Charms.RemoveAt(0);
                }
            }
        }

        if (IsComplete)
        {
            Complete();
        }
    }

    public void Complete()
    {
        DraftPackCompleteEvent?.Invoke(this);
    }
}
