using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "EnvironmentSettings", menuName = "Data/Environment Settings")]
public class EnvironmentSettings : ScriptableObject
{
    [Serializable]
    public enum ServerStack
    {
        Dev,
        Staging,
        Production,
        WASD,
    }
    
    [Serializable]
    public enum MatchmakingType
    {
        Private,
        Public,
    }
    
    [Serializable]
    public class MatchmakerServerHost
    {
        public MatchmakingType MatchmakingType;
        public string QueueName;
    }
    
    [Serializable]
    public class RemoteServerConfig
    {
        public string StackName;
        public ServerStack Stack;
        public string APIServerHost;
        public List<MatchmakerServerHost> MatchmakerServerHosts;
        public bool IsActive;
    }
    
    [Serializable]
    public class LocalServerConfig
    {
        public string APIServerHost;
        public string IP;
        public ushort Port;
    }
    
    private static EnvironmentSettings settings;

    public static EnvironmentSettings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = Resources.Load<EnvironmentSettings>("Settings/EnvironmentSettings");
            }
            return settings;
        }
    }

    [SerializeField] private List<RemoteServerConfig> onlineGameConfigs;
    [SerializeField] public LocalServerConfig LocalGameConfig;
    
    public bool IsWASD => GetActiveRemoteServerConfig().Stack == ServerStack.WASD;
    private RemoteServerConfig GetActiveRemoteServerConfig()
    {
        var activeServer = onlineGameConfigs.Find(item => item.IsActive);
        return activeServer ?? onlineGameConfigs.First();
    }
    
    public string GetAPIServerHost()
    {
        return GameManager.Instance.IsLocalDevelopment ? 
            LocalGameConfig.APIServerHost : GetActiveRemoteServerConfig().APIServerHost;
    }
    
    public string GetMatchmakingQueue(MatchmakingType matchmakingType)
    {
        var activeServerConfig = GetActiveRemoteServerConfig();
        var matchmakerQueue = activeServerConfig.MatchmakerServerHosts.Find(item => item.MatchmakingType == matchmakingType);
        if (matchmakerQueue == null)
        {
            throw new Exception($"Environment config error: can't find matchmaker queue for matchmakingType {matchmakingType} in {activeServerConfig.Stack} server config");
        }

        return matchmakerQueue.QueueName;
    }

    public string GetVersionText(string versionSuffix = "")
    {
        return IsWASD ? $"Stag: {Application.version} Pos: {versionSuffix}" :
            $"{GetActiveRemoteServerConfig().StackName} {Application.version}";
    }
}


