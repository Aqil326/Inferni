using Unity.Netcode;

public struct PlayerData: INetworkSerializable
{
    public ulong Id;
    public string DisplayName;
    public string SelectedCharacterId;
    public string Uid;
    public int PositionId;
    public int RP;
    public int ArenaGroup;
    public string SteamId;
    public int TeamIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref DisplayName);
        serializer.SerializeValue(ref SelectedCharacterId);
        serializer.SerializeValue(ref Uid);
        serializer.SerializeValue(ref PositionId);
        serializer.SerializeValue(ref RP);
        serializer.SerializeValue(ref ArenaGroup);
        serializer.SerializeValue(ref SteamId);
        serializer.SerializeValue(ref TeamIndex);
    }
}
