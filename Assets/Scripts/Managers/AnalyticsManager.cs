using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class AnalyticsManager: BaseManager
{
    public enum EventType
    {
        Error,
        Data,
    }
    
    public enum ErrorType
    {
        None,
        CreateMatchmakerTicketFailed,
        CancelMatchmakerTicketFailed,
        PollingMatchmakerTicketFailed,
        PollingMatchmakerTicketError,
        AllocateGameServerFailed,
        AllocateGameServerTimeout,
        JoinGameServerFailed,
        StartMatchFailed,
        UnexpectedDisconnected,
        ApprovalCheckFailedServerHasStarted,
        ApprovalCheckFailedServerFull,
        ApprovalCheckFailedInvalidClientVersion,
    }

    public enum DataType
    {
        None,
        PlayerNumber,
        GameDuration,
    }

    private string url = string.Empty;
    public static AnalyticsManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        
    }

    private void Start()
    {
        url = $"{EnvironmentSettings.Settings.GetAPIServerHost()}/operationalerrors/report";
    }

    public void SendData(EventType eventType, string message, ErrorType errorType = ErrorType.None, DataType dataType = DataType.None)
    {
        var messageType = eventType == EventType.Error ? errorType.ToString() : dataType.ToString();
        Debug.Log($"Calling Heroku Analytics API with dataType: {messageType}, message: {message}");
        string skipMessage = string.Empty;
#if UNITY_EDITOR
        skipMessage = "Skip calling Heroku Analytics API in Unity Editor";
#else
        if (GameManager.Instance.IsLocalDevelopment)
        {
            skipMessage = "Skip calling Heroku Analytics API for Local Development";    
        }
#endif
        if (!string.IsNullOrEmpty(skipMessage))
        {
            Debug.Log(skipMessage);
            return;
        }
        StartCoroutine(SendPostRequest(messageType, message));
    }
    
    private IEnumerator SendPostRequest(string dataType, string errorMessage)
    {
        var jsonData = new DataPayload
        {
            DataType = dataType,
            Message = errorMessage,
        };
        var json = JsonConvert.SerializeObject(jsonData);
        Debug.Log("Start to send tracking with " + json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Build-Version", Application.version);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                Debug.Log("SendPostRequest success: " + request.downloadHandler.text);
            }
        }
    }
    public class DataPayload
    {
        [JsonProperty("error_type")] public string DataType { get; set; }
        [JsonProperty("error_message")] public string Message { get; set; }
    }
}
