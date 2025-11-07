using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerDeathManager : NetworkBehaviour
{
    public static PlayerDeathManager Instance;
    [SerializeField] private float _deathCooldown;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void HandlePlayerDeath(Player player, MazeChunkSafeZone respawnChunk)
    {
        if (!IsServer) return;

        Debug.Log($"[Server] {player.name} est mort. Respawn dans {_deathCooldown}s.");
        player.gameObject.SetActive(false);

        StartCoroutine(ServerWaitAndRespawn(player, respawnChunk));
    }

    private IEnumerator ServerWaitAndRespawn(Player player, MazeChunkSafeZone respawnChunk)
    {
        yield return new WaitForSeconds(_deathCooldown);

        Vector3 spawnPoint = respawnChunk.transform.position;

        player.transform.position = spawnPoint;
        player.gameObject.SetActive(true);

        player._playerStats._healthComponent.ChangePlayerLife(true);

        RespawnClientRpc(player.OwnerClientId, spawnPoint);
    }

    [ClientRpc]
    private void RespawnClientRpc(ulong playerId, Vector3 newPosition)
    {
        if (NetworkManager.Singleton.LocalClientId != playerId)
            return;

        if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerId) is NetworkObject playerObj)
        {
            if (playerObj.TryGetComponent(out Player localPlayer))
            {
                localPlayer.transform.position = newPosition;
                localPlayer.gameObject.SetActive(true);
                localPlayer._playerStats._healthComponent.ChangePlayerLife(true);
            }
        }
        else
        {
            Debug.LogWarning($"Aucun NetworkObject trouvé pour le joueur {playerId}");
        }
    }
}
