using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource _musicSource;
    public AudioSource _sfxSource;
    public AudioMixer _mixer;
    public SoundLibrary _library;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic(SoundType type)
    {
        var sound = _library.GetSound(type, true);
        if (sound == null) return;
        _musicSource.clip = sound._clip;
        _musicSource.loop = sound._loop;
        _musicSource.Play();
    }

    public void PlaySFX(SoundType type)
    {
        var sound = _library.GetSound(type, false);
        if (sound == null) return;
        _sfxSource.PlayOneShot(sound._clip);
    }

    public void StopMusic()
    {
        _musicSource.Stop();
    }
    
    public void StopSFX()
    {
        _sfxSource.Stop();
    }
}