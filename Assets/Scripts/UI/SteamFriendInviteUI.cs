using Steamworks;
using HeathenEngineering.SteamworksIntegration.UI;
using UnityEngine;

public class SteamFriendInviteUI : MonoBehaviour
{

    [SerializeField]
    private FriendProfile friendProfile;

    private SteamLobbyManager steamLobbyManager;

#if !NOSTEAMWORKS
    private void Init()
    {
        if (steamLobbyManager == null)
        {
            steamLobbyManager = GameManager.GetManager<SteamLobbyManager>();    
        }
    }
    
    public void InviteSteamFriend()
    {
        Init();
        steamLobbyManager.InviteFriendToLobby(friendProfile.UserData.id);
    }

    public void ChatWithFriend()
    {
        Init();
        steamLobbyManager.ChatWithFriend(friendProfile.UserData.id);
    }
#endif

}
