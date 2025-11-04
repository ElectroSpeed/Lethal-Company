using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

[CustomEditor(typeof(SoundLibrary))]
public class SoundLibraryEditor : Editor
{
    private const string ENUM_PATH = "Assets/Script/Audio/SoundType.cs";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);
        if (GUILayout.Button("Mettre à jour l'Enum SoundType"))
            UpdateEnum();
    }

    private void UpdateEnum()
    {
        SoundLibrary library = (SoundLibrary)target;
        var soundNames = library._musicSounds
            .Concat(library._sfxSounds)
            .Where(s => !string.IsNullOrEmpty(s._name))
            .Select(s => SanitizeEnumName(s._name))
            .Distinct()
            .ToList();
        if (soundNames.Count == 0)
        {
            return;
        }
        string enumCode = $@"public enum SoundType
        {{
            None,
            {string.Join(",\n    ", soundNames)}
        }}";
        
        File.WriteAllText(ENUM_PATH, enumCode);
        AssetDatabase.Refresh();
        Debug.Log($"Enum SoundType mis à jour ({soundNames.Count} sons).");
    }

    private string SanitizeEnumName(string input)
    {
        string clean = new string(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (string.IsNullOrEmpty(clean))
            clean = "Unnamed";
        return clean;
    }
}