using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public List<AudioClip> sounds;
    public static SoundManager instance;
    private AudioSource audioSource;

    // Unity's Awake method.
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Play a specified audio clip by name
    public void PlaySound(string clipName)
    {
        if (audioSource != null)
        {
            AudioClip clipToPlay = sounds.Find(sound => sound.name == clipName);
            if (clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("Sound clip not found: " + clipName);
            }
        }
    }
}
