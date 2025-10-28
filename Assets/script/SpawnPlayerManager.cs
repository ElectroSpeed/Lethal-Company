using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpawnPlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _playerSpawnPos;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            OnClientConnected(clientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        StartCoroutine(SpawnPlayer(clientId));
    }

    private IEnumerator SpawnPlayer(ulong clientId)
    {
        yield return new WaitUntil(() => GetComponent<MapManager>()._safeChunk != null);

        GameObject playerLobby = Instantiate(_playerPrefab, GetComponent<MapManager>()._safeChunk.transform.position + new Vector3(0, 2, 0), Quaternion.identity);

        var netObj = playerLobby.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);
    }
}
