using System.Collections;
using System.Collections.Generic;
using SimpleAudioManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{

    public static LoadingManager Instance;
    [SerializeField] private Image loadingBarImage;
    [SerializeField] private CanvasGroup canvasGroup;
#if UNITY_SERVER
    private bool isServer = true;
#else
    private bool isServer = false;
#endif
#if UNITY_EDITOR
    private bool IsEditor = true;
#else
    private bool IsEditor = false;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log("Can only be one Loading Manager");
            Destroy(this);
        }

        HideUI();
    }

    public void StartLoad(string sceneName)
    {
        GarbageCollect();
        StartCoroutine(AsyncLoadLevel(sceneName));
    }

    private void GarbageCollect()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    IEnumerator AsyncLoadLevel(string sceneName)
    {
        //Start load
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        ShowUI();
        //Debug.Log("Load started");

        //Get load progress
        while (!loadOperation.isDone)
        {
            float _loadProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            loadingBarImage.fillAmount = _loadProgress;
            //Debug.Log("Load progress: " + _loadProgress);
            yield return new WaitForEndOfFrame();
        }

        //Load ended
        //Debug.Log("Load finished");
        HideUI();

        //Play music
        switch (sceneName)
        {
            case "MainScene":
                if (!isServer)
                {            
                    // probably redundant
                    // SongManager.instance.PlaySong(0);
                    // SongManager.instance.SetIntensity(3);
                }     
                break;
        }
    }

    private void HideUI()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ShowUI()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        loadingBarImage.fillAmount = 0f;
    }

}
