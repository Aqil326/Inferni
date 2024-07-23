using Nakama;
using System;
using System.Collections;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
#endif
using UnityEngine;

public class NakamaUserManager : MonoBehaviour
{
    #region FIELDS

    private IApiAccount account = null;

    #endregion

    #region EVENTS

    public event Action onLoaded = null;

    #endregion

    #region PROPERTIES

    public static NakamaUserManager Instance { get; private set; }
    public bool LoadingFinished { get; private set; } = false;
    public IApiUser User { get => account.User; }
    public string DisplayName { get => account.User.DisplayName; }

    #endregion

    #region BEHAVIORS

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        NakamaNetworkManager.Instance.onLoginSuccess += AutoLoad;
    }

    private void OnDestroy()
    {
        NakamaNetworkManager.Instance.onLoginSuccess -= AutoLoad;
    }

    private async void AutoLoad()
    {
        account = await NakamaNetworkManager.Instance.Client.GetAccountAsync(NakamaNetworkManager.Instance.Session);
        LoadingFinished = true;
        onLoaded?.Invoke();
    }

    public void UpdateDisplayNameAndFindMatch(string displayName, string characterId)
    {
        Task task = FindMatchAfterUpdateName(displayName, characterId);
    }

    public async Task FindMatchAfterUpdateName(string displayName, string characterId)
    {
        if (displayName.Length > 200)
        {
            displayName = displayName.Substring(0, 199);
        }

        await NakamaNetworkManager.Instance.Client.UpdateAccountAsync(NakamaNetworkManager.Instance.Session, null, displayName + "#" + characterId);

        account = await NakamaNetworkManager.Instance.Client.GetAccountAsync(NakamaNetworkManager.Instance.Session);

        NakamaNetworkManager.Instance.onMatchJoin += NakamaNetworkManager.Instance.JoinedMatch;
        NakamaNetworkManager.Instance.onMatchJoin += PlayersNakamaNetworkManager.Instance.MatchJoined;
        PlayersNakamaNetworkManager.Instance.onPlayerJoined += NakamaNetworkManager.Instance.PlayerJoined;
        PlayersNakamaNetworkManager.Instance.onPlayerLeft += NakamaNetworkManager.Instance.PlayerLeft;
        PlayersNakamaNetworkManager.Instance.onPlayersReceived += NakamaNetworkManager.Instance.PlayersReceived;
        NakamaNetworkManager.Instance.UpdateStatus();
        NakamaNetworkManager.Instance.JoinMatchAsync();
    }

    #endregion
}