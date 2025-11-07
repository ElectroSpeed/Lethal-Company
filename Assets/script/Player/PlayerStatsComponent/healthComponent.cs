using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


[System.Serializable]
public class HealthComponent : MonoBehaviour
{
    public bool _isAlive { get; private set; }
    public void ChangePlayerLife(bool newValue) => _isAlive = newValue;

    [SerializeField] private float _deathCooldown = 5.0f;

    public void Die(MazeChunkSafeZone respawnChunk, Player player)
    {
        Debug.Log("player die");
        _isAlive = false;
        gameObject.SetActive(false);

        StartCoroutine(WaitForPlayerRespawn(respawnChunk, player));
    }
    private IEnumerator WaitForPlayerRespawn(MazeChunkSafeZone respawnChunk, Player player)
    {

        yield return new WaitForSeconds(_deathCooldown);


        Vector3 spawnPoint = respawnChunk._bounds.center;
        transform.position = spawnPoint;
        gameObject.SetActive(true);
    }
}