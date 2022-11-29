using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private AudioSource audioSource;
    public AudioMixer audioMixer;

    public AudioClip swipe;
    public AudioClip click;
    public AudioClip clear;

    void Start()
    {
        audioSource = transform.Find("Sound").GetComponent<AudioSource>();
    }

    public void PlaySoundSwipe()
	{
        audioSource.PlayOneShot(swipe);
	}

    public void PlaySoundClick()
    {
        audioSource.PlayOneShot(click);
    }

    public void PlaySoundClear()
    {
        audioSource.PlayOneShot(clear);
    }

    public void ToogleMusic(Toggle toggle)
	{
        audioSource.PlayOneShot(click);

        if (toggle.isOn)
            audioMixer.SetFloat("MusicVolume", 0);
        else
            audioMixer.SetFloat("MusicVolume", -80);
    }

    public void ToogleSound(Toggle toggle)
    {
        audioSource.PlayOneShot(click);

        if (toggle.isOn)
            audioMixer.SetFloat("SoundVolume", 0);
        else
            audioMixer.SetFloat("SoundVolume", -80);
    }

    public void ChangeMusic(Slider slider)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Lerp(-25, 0, slider.value));
    }

    public void ChangeSound(Slider slider)
    {
        audioMixer.SetFloat("SoundVolume", Mathf.Lerp(-25, 0, slider.value));
    }
}
