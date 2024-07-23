using System;
using System.Collections.Generic;
using GameAnalyticsSDK;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using Utils;

using TMPro;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using System.Threading.Tasks;



/// <summary>
    /// An example of how to use SQP from the server using the Multiplay SDK.
    /// The ServerQueryHandler reports the given information to the Multiplay Service.
    /// </summary>
public class Example_ServerQueryHandler : MonoBehaviour
{
    private const ushort k_DefaultMaxPlayers = 8;
    private const string k_DefaultServerName = "InferniGameServer";
    private const string k_DefaultGameType = "MyGameType";
    private const string k_DefaultBuildId = "MyBuildId";
    private const string k_DefaultMap = "MyMap";

    public ushort currentPlayers;

    // Should this be static?
    private IServerQueryHandler m_ServerQueryHandler;

    private async void Start()
    {
#if UNITY_SERVER        
        Debug.Log("Example_ServerQueryHandler starting");
        m_ServerQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(k_DefaultMaxPlayers, k_DefaultServerName, k_DefaultGameType, k_DefaultBuildId, k_DefaultMap);
        Debug.Log("Example_ServerQueryHandler finished");
#endif
    }

    private void Update()
    {
       // UpdatePublic();
    }

    public void UpdatePublic()
    {
        if (m_ServerQueryHandler != null)
        {
            // Debug.Log("Example_ServerQueryHandler updating Server check, currentPlayers = " + currentPlayers);
            m_ServerQueryHandler.CurrentPlayers = currentPlayers;
            m_ServerQueryHandler.UpdateServerCheck();
        }
    }

    public void ChangeQueryResponseValues(ushort maxPlayers, string serverName, string gameType, string buildId)
    {
        m_ServerQueryHandler.MaxPlayers = maxPlayers;
        m_ServerQueryHandler.ServerName = serverName;
        m_ServerQueryHandler.GameType = gameType;
        m_ServerQueryHandler.BuildId = buildId;
    }

    public void PlayerCountChanged(ushort newPlayerCount)
    {
        m_ServerQueryHandler.CurrentPlayers = newPlayerCount;
    }

}