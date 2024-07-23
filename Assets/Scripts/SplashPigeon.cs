using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashPigeon : MonoBehaviour
{

    //private AudioSource audioSource;
    [SerializeField] private SoundManager.Sound birdShit;
    [SerializeField] private SplashManager splashManager;

    public void BeginFadeOut()
    {
        splashManager.BeginFade();
    }

    public void DoBirdShit()
    {
        birdShit.Play(false);
    }
}
