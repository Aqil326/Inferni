using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public struct AnimationWithDuration
{
    [SpineAnimation]
    public string Name;
    public float Duration;
}

public class EndScreen : MonoBehaviour
{
    [SerializeField]
    public List<AnimationWithDuration> winAnimations;
    [SerializeField]
    public List<AnimationWithDuration> loseAnimations;
    [SerializeField]
    private CanvasGroup canvasGroup;
    [SerializeField]
    private Button spectateButton;
    [SerializeField]
    private Button leaveButton;

    private Queue<AnimationWithDuration> winAnimationQueue;
    private Queue<AnimationWithDuration> loseAnimationQueue;
    
    private Spine.AnimationState spineAnimationState;
    private SkeletonAnimation skeletonAnimation;
    private Coroutine animationCoroutine;
    private TrackEntry currentEntry;
    
    private const string CtaSceneName = "CTAScene";
    
    private void Start()
    {
        winAnimationQueue = new Queue<AnimationWithDuration>(winAnimations);
        loseAnimationQueue = new Queue<AnimationWithDuration>(loseAnimations);
        
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        spineAnimationState = skeletonAnimation.AnimationState;
        
        GameManager.Instance.GameWonEvent += ShowGameVictory;
        GameManager.Instance.GameLostEvent += ShowGameDefeat;
        ResetAnimations();

        spectateButton.onClick.AddListener(Hide);
        leaveButton.onClick.AddListener(EndMatch);
    }

    private void OnDestroy()
    {
        ResetAnimations();
    }
    
    private void ResetAnimations()
    {
        gameObject.SetActive(false);
        currentEntry = null;
        SetGamePlayerUI(false);
        spectateButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);
        if (animationCoroutine == null) return;
        
        StopCoroutine(animationCoroutine);
        animationCoroutine = null;
        if (spineAnimationState != null)
        {
            spineAnimationState.ClearTrack(0); 
        }
    }   

    private void SetGamePlayerUI(bool isHide)
    {
        if (canvasGroup == null) return;
        
        if (isHide)
        {
            canvasGroup.alpha = 0.1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            spectateButton.gameObject.SetActive(true);
            leaveButton.gameObject.SetActive(true);
        }
        else
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void ShowGameDefeat()
    {
        if (loseAnimations?.Count == 0) return;
        
        ResetAnimations();
        SetGamePlayerUI(true);
        gameObject.SetActive(true);
        animationCoroutine = StartCoroutine(DoAnimationsRoutine(loseAnimationQueue));
    }

    private void ShowGameVictory()
    {
        if (winAnimations?.Count == 0) return;
        
        ResetAnimations();
        SetGamePlayerUI(true);
        gameObject.SetActive(true);
        animationCoroutine = StartCoroutine(DoAnimationsRoutine(winAnimationQueue));
    }

    IEnumerator DoAnimationsRoutine(Queue<AnimationWithDuration> animationQueue)
    {
        while (animationQueue.Count > 0)
        {
            var nextAnimation = animationQueue.Dequeue();
            bool shouldLoop = animationQueue.Count > 0;
            currentEntry = spineAnimationState.SetAnimation(0, nextAnimation.Name, shouldLoop);
            if (!shouldLoop)
            {
                currentEntry.TimeScale = 1.5f;
            }
            yield return new WaitForSeconds(nextAnimation.Duration);
        }

        if (currentEntry != null)
        {   
            currentEntry.TimeScale = 0;
        }

        if(GameManager.Instance.IsGameEnd)
        {
            EndMatch();
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        SetGamePlayerUI(false);
    }

    public void EndMatch()
    {
#if IS_DEMO
        GameManager.Instance.EndMatch(CtaSceneName);
#else
        GameManager.Instance.EndMatch();
#endif
    }
}
