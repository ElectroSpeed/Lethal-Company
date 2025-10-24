using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


[System.Serializable]
public class HealthComponent
{
    public bool _isAlive { get; private set; }
    public void ChangePlayerLife(bool newValue) => _isAlive = newValue;


    public void Spawn()
    {

    }

    public void Die()
    {

    }
    public void PlayerDown()
    {

    }
}