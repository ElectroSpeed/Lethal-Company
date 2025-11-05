using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    public List<Sound> _musicSounds = new();
    public List<Sound> _sfxSounds = new();

    public Sound GetSound(SoundType type, bool isMusic)
    {
        string name = type.ToString();
        var list = isMusic ? _musicSounds : _sfxSounds;
        foreach (var s in list)
            if (s._name == name)
                return s;
        Debug.LogWarning($"Son {name} introuvable dans {(isMusic ? "Music" : "SFX")}.");
        return null;
    }
}