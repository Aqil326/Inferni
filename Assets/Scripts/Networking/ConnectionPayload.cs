public class ConnectionPayload
{

    public string SteamID;
    public string AppVersion;
    public string LobbyID;
    public int PositionId;

    public ConnectionPayload(string steamID, string appVersion, string lobbyID, int positionId)
    {
        SteamID = steamID;
        AppVersion = appVersion;
        LobbyID = lobbyID;
        PositionId = positionId; // WASD
    }

    public string ToString()
    {
        return "SteamID = " + SteamID + " , AppVersion = " + AppVersion + " , LobbyID = " + LobbyID + " , PositionId = " + PositionId;
    }

}
