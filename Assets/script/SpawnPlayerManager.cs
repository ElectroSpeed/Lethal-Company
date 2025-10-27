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
        GameObject playerLobby = Instantiate(_playerPrefab, _playerSpawnPos.position, Quaternion.identity);

        var netObj = playerLobby.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);
    }
}
