using System;
using Unity.Netcode;
using UnityEngine;

public struct DamageHistoryRecord : INetworkSerializable
{
    public DateTime time;
    public int damage;
    public int sourceCharacterIndex;
    public int targetCharacterIndex;

    public DamageHistoryRecord(DateTime time, Card card, Character source, Character target, int damage)
    {
        this.time = time;
        this.damage = damage;
        this.sourceCharacterIndex = source.Index;
        this.targetCharacterIndex = target.Index;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref time);
        serializer.SerializeValue(ref damage);
        serializer.SerializeValue(ref sourceCharacterIndex);
        serializer.SerializeValue(ref targetCharacterIndex);
    }
}