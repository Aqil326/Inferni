using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class VersionChecker : MonoBehaviour
{
    private string versionCheckUrl = "https://village-inferni.s3.eu-west-2.amazonaws.com/ClientSettingUpdate.json";

    public static bool IsVersionValid { get; private set; } = true;

    public static bool VersionCheckFinished { get; private set; } = false;

    public static event Action VersionCheckFinishedEvent;

#if IS_DEMO
    private static List<PlaytestWindow> playtestWindows;

    public static bool ShouldCheckPlaytestWindows { get; private set; } = true;

    public static DateTime? GetPlaytestWindowStart()
    {
        return GetDate(true);
    }

    public static DateTime? GetPlaytestWindowEnd()
    {
        return GetDate(false);
    }

    private static DateTime? GetDate(bool isStart)
    {
        DateTime? startDate = null;
        DateTime? endDate = null;
        var now = DateTime.UtcNow;
        bool hasCurrentDate = false;
        if (playtestWindows != null)
        {
            foreach (var window in playtestWindows)
            {
                if (window.timeStart < now && window.timeEnd > now)
                {
                    hasCurrentDate = true;

                    if (!startDate.HasValue || startDate.Value > window.timeStart)
                    {
                        startDate = window.timeStart;
                        endDate = window.timeEnd;
                    }
                }
                else if (window.timeStart > now && !hasCurrentDate)
                {
                    if (!startDate.HasValue || startDate.Value > window.timeStart)
                    {
                        startDate = window.timeStart;
                        endDate = window.timeEnd;
                    }
                }
            }
        }
        if(isStart)
        {
            return startDate;
        }
        return endDate;
    }
#endif

    private void Start()
    {
        if(Application.version.Contains("X"))
        {
            //skip version check
            VersionCheckFinished = true;
            IsVersionValid = true;
            return;
        }
        StartCoroutine(GetRequest(versionCheckUrl));
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                Debug.Log($"Version Check Json received : {webRequest.downloadHandler.text}");

                VersionCheckFinished = true;

                var versionCheckData = JsonConvert.DeserializeObject<VersionCheck>(webRequest.downloadHandler.text);

                foreach (var windows in versionCheckData.playtestWindowsPerClientVersion)
                {
                    if (windows.clientVersion == Application.version)
                    {
                        IsVersionValid = true;
#if IS_DEMO
                        ShouldCheckPlaytestWindows = !windows.disablePlaytestWindowsChecking;
                        playtestWindows = windows.playtestWindows;
#endif
                        VersionCheckFinishedEvent?.Invoke();
                        yield break;
                    }
                }
                VersionCheckFinishedEvent?.Invoke();
                IsVersionValid = false;
            }
        }
    }

#if IS_DEMO
    public static void DebugCurrentPlaytestWindow()
    {
        if(!Debug.isDebugBuild)
        {
            return;
        }
        var window = new PlaytestWindow();
        window.timeStart = DateTime.UtcNow;
        window.timeEnd = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        playtestWindows.Clear();
        playtestWindows.Add(window);
    }


    public static void DebugNextPlaytestWindow()
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }
        var window = new PlaytestWindow();
        window.timeStart = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        window.timeEnd = DateTime.UtcNow + TimeSpan.FromHours(1);
        playtestWindows.Clear();
        playtestWindows.Add(window);
    }
#endif

    [Serializable]
    public class PlaytestWindow
    {
        [JsonProperty("time_Start")]
        public DateTime timeStart;
        [JsonProperty("time_End")]
        public DateTime timeEnd;
    }

    [Serializable]
    public class PlaytestWindowsPerClientVersion
    {
        [JsonProperty("client_version")]
        public string clientVersion;

        [JsonProperty("playtest_windows")]
        public List<PlaytestWindow> playtestWindows;

        [JsonProperty("disable_playtest_windows_checking")]
        public bool disablePlaytestWindowsChecking;
    }

    [Serializable]
    public class VersionCheck
    {
        [JsonProperty("playtest_windows")]
        public List<PlaytestWindowsPerClientVersion> playtestWindowsPerClientVersion;
    }
}
