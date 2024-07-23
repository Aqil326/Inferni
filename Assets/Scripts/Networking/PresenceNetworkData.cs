using Newtonsoft.Json;
public class PresenceNetworkData
{
    #region FIELDS

    private const string SessionIdKey = "sessionId";


    #endregion

    #region PROPERTIES

    [JsonProperty(SessionIdKey)] public string SessionId { get; private set; }

    #endregion

    #region CONSTRUCTORS

    public PresenceNetworkData(string sessionId, string userId)
    {
        SessionId = sessionId;
    }

    #endregion
}