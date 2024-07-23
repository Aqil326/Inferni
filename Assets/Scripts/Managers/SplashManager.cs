using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleAudioManager;

public class SplashManager : MonoBehaviour
{


#if UNITY_SERVER
    private bool IsServer = true;
#else
    private bool IsServer = false;
#endif
#if UNITY_EDITOR
    private bool IsEditor = true;
#else
    private bool IsEditor = false;
#endif

    [SerializeField] private FadeImage fadeImage;
    [SerializeField] private float fadeTime = 2f;
    private bool inFade = false;


    private void Awake()
    {
       // Screen.fullScreen = true;
    }

    private void Start()
    {
        if (IsServer || IsEditor)
        {
            EndScene();
        }
    }


    public void BeginFade()
    {
        if (inFade) return;
        fadeImage.DoFade(fadeTime);
        StartCoroutine("BeginEnd");
        inFade = true;
    }

    private IEnumerator BeginEnd()
    {
        yield return new WaitForSeconds(fadeTime);
        EndScene();
    }

    private void Update()
    {
        if (Input.anyKey)
        {
            BeginFade();
        }
    }

    private void EndScene()
    {
        //SceneManager.LoadScene("InitScene", LoadSceneMode.Single);
        LoadingManager.Instance.StartLoad("MainScene");
    }

}
