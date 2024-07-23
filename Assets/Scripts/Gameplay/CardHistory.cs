using System;
using Unity.Netcode;
using UnityEngine;

public struct CardHistoryRecord : INetworkSerializable
{
    public DateTime time;
    public string cardId;

    public CardHistoryRecord(DateTime time, Card card)
    {
        this.time = time;
        this.cardId = card.Data.InternalID;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref time);
        serializer.SerializeValue(ref cardId);
    }
}