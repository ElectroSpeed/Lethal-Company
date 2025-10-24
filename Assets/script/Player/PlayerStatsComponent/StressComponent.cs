using UnityEngine;

[System.Serializable]

public class StressComponent
{
    public bool IsStressed() => _currentStress >= _maxStress;


    [Header("Player Current Stress")]
    public float _currentStress { get; private set; }
    public void SetCurrentStress(float value) => _currentStress = value;



    [Header("Player Max Stress")]
    public float _maxStress { get; private set; }
    public void SetMaxStress(float value) => _currentStress = value;



    [Header("Player Min Stress")]
    public float _minStress { get; private set; }
    public void SetMinStress(float value) => _currentStress = value;



    public StressComponent(float currentStress = 0.0f, float maxStress = 100.0f, float minStress = 0.0f)
    {
        _currentStress = currentStress;
        _maxStress = maxStress;
        _minStress = minStress;
    }
}