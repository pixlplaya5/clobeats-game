using System.Collections.Generic;
using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    public static SFXPlayer Instance;
    public AudioSource audioSource;
    public Dictionary<string, AudioClip> loadedAudioClips = new Dictionary<string, AudioClip>();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("AudioSource not found on this GameObject or assigned in Inspector.");
                enabled = false; // Disable script if no AudioSource
                return;
            }
        }
    }
    
    public void PlayClip(string clipName)
    {
        if (loadedAudioClips.TryGetValue(clipName, out var clip))
        {
            audioSource.clip = null;
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogError("[SFXPlayer] Audio clip '" + clipName + "' is not loaded.");
        }
    }

    public void PlayFrozenClip(string clipName)
    {
        if (loadedAudioClips.TryGetValue(clipName, out var clip))
        {
            audioSource.clip = clip;
            audioSource.Play();
            while (audioSource.time < clip.length)
            {
                // freeze and do nothing
            }
        }
        else
        {
            Debug.LogError("[SFXPlayer] Audio clip '" + clipName + "' is not loaded.");
        }
    }

    public void LoadClip(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>(clipName);
        if (clip != null)
        {
            loadedAudioClips.Add(clipName, clip);
        }
        else
        {
            Debug.LogError("[SFXPlayer] Audio clip '" + clipName + "' does not exist in resources.");
        }
    }
    public void LoadActualClip(AudioClip clip1)
    {
        if (clip1 != null)
        {
            loadedAudioClips.Add(clip1.name, clip1);
        }
        else
        {
            Debug.LogError("[SFXPlayer] Audio clip '" + clip1.name + "' does not exist.");
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
