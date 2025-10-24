
using UnityEngine;

[System.Serializable]
public class SpeedComponent
{
    [Header("Player Current Speed ")]
    public float _currentSpeed { get; private set; }
    public void SetCurrentSpeed(float value) => _currentSpeed = value;



    [Header("Player Max Speed ")]
    public float _maxSpeed { get; private set; }
    public void SetMaxSpeed(float value) => _currentSpeed = value;



    [Header("Player Min Speed ")]
    public float _minSpeed { get; private set; }
    public void SetMinSpeed(float value) => _currentSpeed = value;

    public SpeedComponent(float maxSpeed, float currentSpeed = 0.0f, float minSpeed = 0.0f)
    {
        _currentSpeed = currentSpeed;
        _maxSpeed = maxSpeed;
        _minSpeed = minSpeed;
    }

}