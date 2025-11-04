using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    public static AudioSettings Instance;

    public AudioMixer _mixer;
    public Slider _masterSlider;
    public Slider _musicSlider;
    public Slider _sfxSlider;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        if (_masterSlider) _masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (_musicSlider) _musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (_sfxSlider) _sfxSlider.onValueChanged.AddListener(SetSfxVolume);

        _mixer.SetFloat("Master", PlayerPrefs.GetFloat("MasterVolume", 0));
        _mixer.SetFloat("Music", PlayerPrefs.GetFloat("MusicVolume", 0));
        _mixer.SetFloat("SFX", PlayerPrefs.GetFloat("SFXVolume", 0));

        if (_masterSlider) _masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0);
        if (_musicSlider) _musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0);
        if (_sfxSlider) _sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0);
    }

    public void SetMasterVolume(float value)
    {
        _mixer.SetFloat("Master", value);
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        _mixer.SetFloat("Music", value);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSfxVolume(float value)
    {
        _mixer.SetFloat("SFX", value);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }
}