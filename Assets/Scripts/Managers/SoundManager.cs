using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SoundManager : MonoBehaviour
{

    public static SoundManager Instance;

    [System.Serializable]
    public class Sound
    {
        public AudioClip[] audioClips;
        [Range(0f, 1f)] public float volume = 1;

        //Play non-spatial (e.g. UI)
        public void Play(bool loop)
        {
            Instance.Play(this, loop);
        }

        //Play spatial (e.g. in-game SFX)
        public void Play(bool loop, Vector3 position)
        {
            Instance.Play(this, loop, position);
        }

        //Play on own audiosource (e.g. on permenant GameObject)
        public void Play(bool loop, AudioSource audioSource)
        {
            Instance.Play(this, loop, audioSource);
        }
    }

    [SerializeField] private AudioSource spatialPrefab;
    private List<AudioSource> spatialSources;
    [SerializeField] private AudioSource nonSpatialPrefab;
    private List<AudioSource> nonSpatialSources;


    private bool mute = false;
    [SerializeField] private TextMeshProUGUI muteButtonTMP;
    [SerializeField] Sound unmuteSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log("Can only be one Sound Manager");
            Destroy(this);
        }

        muteButtonTMP.text = "Mute";

        spatialSources = new List<AudioSource>();
        nonSpatialSources = new List<AudioSource>();
    }

    //Non-spatial
    public void Play(Sound sound, bool loop)
    {
        //Check if it's a one shot
        if (!CheckSoundValidity(sound)) return;
        AudioSource source = GetSource(false);
        source.loop = loop;
        source.clip = sound.audioClips[Random.Range(0, sound.audioClips.Length)];
        source.volume = sound.volume;
        source.Play();
    }

    //Spatial
    public void Play(Sound sound, bool loop, Vector3 position)
    {
        if (!CheckSoundValidity(sound)) return;
        AudioSource source = GetSource(true);
        source.gameObject.transform.position = position;
        source.loop = loop;
        source.clip = sound.audioClips[Random.Range(0, sound.audioClips.Length)];
        source.volume = sound.volume;
        source.Play();
    }

    //Own source
    public void Play(Sound sound, bool loop, AudioSource source)
    {
        if (!CheckSoundValidity(sound)) return;
        source.loop = loop;
        source.clip = sound.audioClips[Random.Range(0, sound.audioClips.Length)];
        source.volume = sound.volume;
        source.Play();
    }

    private bool CheckSoundValidity(Sound sound)
    {
        if (sound.audioClips.Length == 0)
        {
            Debug.LogWarning("No AudioClips in sound.");
            return false;
        }

        foreach(AudioClip clip in sound.audioClips)
        {
            if (clip == null)
            {
                Debug.LogWarning("Missing AudioClip.");
                return false;
            }
        }

        return true;
    }

    public void ToggleMute()
    {
        if (mute) Unmute(); else Mute();
    }

    private void Mute()
    {
        muteButtonTMP.text = "Unmute";
        AudioListener.volume = 0f;
        mute = true;
    }

    private void Unmute()
    {
        muteButtonTMP.text = "Mute";
        AudioListener.volume = 1f;
        mute = false;
        unmuteSound.Play(false);
    }

    private AudioSource GetSource(bool spatial)
    {
        if (spatial)
        {
            if (spatialSources.Count > 0)
            {
                foreach (AudioSource source in spatialSources)
                {
                    if (!source.isPlaying) return source;
                }
            }

            spatialSources.Add(Instantiate(spatialPrefab, transform));
            return spatialSources[spatialSources.Count-1] ;
        }
        else
        {
            if (nonSpatialSources.Count > 0)
            {
                foreach (AudioSource source in nonSpatialSources)
                {
                    if (!source.isPlaying) return source;
                }
            }

            nonSpatialSources.Add(Instantiate(nonSpatialPrefab, transform));
            return nonSpatialSources[nonSpatialSources.Count-1];
        }

    }

    private void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        foreach (AudioSource source in spatialSources) source.volume = volume;
        foreach (AudioSource source in nonSpatialSources) source.volume = volume;
    }

}