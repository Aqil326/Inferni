using DG.Tweening;
using UnityEngine;

public class MirrorSpinnerManager : MonoBehaviour
{
    [SerializeField]
    public MirrorSpinner mirrorPrefab;
    public int numberOfMirrors = 6;
    public float radius = 1.0f;
    public float angularSpeed = 60.0f;

    private CharacterView characterView;
    private Transform centerTransform;
    private MirrorSpinner[] mirrors;

    private void OnDestroy()
    {
        if (characterView != null)
        {
            characterView.PauseEvent -= OnCharacterPause;
            characterView.UnpauseEvent -= OnCharacterUnpause;
        }

        if (mirrors == null) return;
        
        foreach (var mirror in mirrors)
        {
            if (mirror != null) Destroy(mirror.gameObject);
        }
    }

    public void ShowMirrors(CharacterView characterView, Transform playerTransform)
    {
        this.characterView = characterView;
        characterView.PauseEvent += OnCharacterPause;
        characterView.UnpauseEvent += OnCharacterUnpause;
        centerTransform = playerTransform;
        
        mirrors = new MirrorSpinner[numberOfMirrors];
        for (var i = 0; i < numberOfMirrors; i++)
        {
            var angle = i * (360f / numberOfMirrors);
            var offset = Quaternion.Euler(0, angle, 0) * (Vector3.forward * radius);
            mirrors[i] = Instantiate(mirrorPrefab, centerTransform.position + offset, Quaternion.identity);
            mirrors[i].transform.SetParent(playerTransform);
            mirrors[i].transform.localScale = Vector3.one * 0.5f;
                
            mirrors[i].gameObject.SetActive(true);
            mirrors[i].ShowMirrors((360f / numberOfMirrors), i, playerTransform, radius, angularSpeed);
        }
    }

    private void OnCharacterPause()
    {
        foreach(var mirror in mirrors)
        {
            mirror.Pause();
        }
    }

    private void OnCharacterUnpause()
    {
        foreach (var mirror in mirrors)
        {
            mirror.Unpause();
        }
    }
}
