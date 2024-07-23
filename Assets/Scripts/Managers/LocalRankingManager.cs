using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class LocalRankingManager : BaseManager
{
    private string url = string.Empty;
    private readonly Dictionary<int, int> RankAndArenaMapping = new()
    {
        { 0, 0 },
        { 1, 0 },
        { 2, 1 },
        { 3, 1 },
        { 4, 1 },
        { 5, 2 },
        { 6, 2 },
        { 7, 2 },
        { 8, 3 },
        { 9, 3 },
        { 10, 3 },
    };
    private string userId = "";
    
    public static event Action OnRPRetrieved;
    public RankingData RankingData { get; private set; }
    
    private void Start()
    {
        url = $"{EnvironmentSettings.Settings.GetAPIServerHost()}/profiles/checkRP";
        RankingData = new RankingData();
    }

    public void Init(string playerId)
    {
        userId = playerId;
        if (string.IsNullOrEmpty(userId)) return;
        var uri = $"{url}?steam_id={playerId}";
        Debug.Log("Calling CheckRP API: " + uri);
        
        if (GameManager.Instance.DisableRP)
        {
            Debug.Log("Skip CheckRP API call due to DisableRP");
            return;
        }
        
        StartCoroutine(GetRequest(uri));
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                Debug.Log($"PR checking Json received : {webRequest.downloadHandler.text}");
                RankingData = JsonConvert.DeserializeObject<RankingData>(webRequest.downloadHandler.text);
                RankingData.ArenaGroup = RankAndArenaMapping.TryGetValue(RankingData.PlayerRank, out var arenaGroup) ?
                        arenaGroup : RankAndArenaMapping.First().Value;
                OnRPRetrieved?.Invoke();
            }
        }
    }
}

public class RankingData
{
    [JsonProperty("steam_id")] public string SteamId;
    [JsonProperty("player_name")] public string PlayerName;
    [JsonProperty("rp_id")] public int RP;
    [JsonProperty("arena")] public int PlayerRank; // TODO: update json property after BE deployed
    public int ArenaGroup;
}
