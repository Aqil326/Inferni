using Newtonsoft.Json;

public class PlayerNetworkData
{
    #region FIELDS

    private const string PresenceKey = "presence";
    private const string DisplayNameKey = "displayName";
    private const string CharacterIdKey = "characterId";

    #endregion

    #region PROPERTIES

    [JsonProperty(PresenceKey)] public PresenceNetworkData Presence { get; private set; }
    [JsonProperty(DisplayNameKey)] public string DisplayName { get; private set; }
    [JsonProperty(CharacterIdKey)] public string CharacterId;

    #endregion

    #region CONSTRUCTORS

    public PlayerNetworkData(PresenceNetworkData presence, string displayName, string characterId)
    {
        Presence = presence;
        DisplayName = displayName.Split("#")[0];
        //TODO: Change Character ID collecting
        CharacterId = displayName.Split("#")[1];
    }

    #endregion
}

