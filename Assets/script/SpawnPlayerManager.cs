using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpawnPlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private MapManager _mapManager;
    [SerializeField] private GameLoop _gameLoop;

    private bool _mapGenerated;

    private void Awake()
    {
        if (_mapManager == null)
            _mapManager = GetComponent<MapManager>();

    }

    private void OnEnable()
    {
        EventBus.Subscribe<bool>(EventType.MapGenerated, OnMapGenerated);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<bool>(EventType.MapGenerated, OnMapGenerated);
    }

    private void OnMapGenerated(bool value)
    {
        if (!IsServer) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            OnClientConnected(clientId);
        }
    }


    private void OnClientConnected(ulong clientId)
    {
        StartCoroutine(SpawnPlayerPrefab(clientId));
    }

    private IEnumerator SpawnPlayerPrefab(ulong clientId)
    {
        yield return new WaitForEndOfFrame();


        GameObject player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
        Vector3 spawnPos = GetSpawnPointWithPlayerID(player, clientId);
        player.GetComponent<Rigidbody>().position = spawnPos;

        _gameLoop._players.Add(player.GetComponent<Player>());

        var netObj = player.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            netObj = player.AddComponent<NetworkObject>();
        }
        netObj.SpawnAsPlayerObject(clientId);
    }

    private Vector3 GetSpawnPointWithPlayerID(GameObject player, ulong playerIndex)
    {
        if (_mapManager == null || _mapManager._safeChunk == null)
            return Vector3.zero;

        var chunk = _mapManager._safeChunk;

        Transform spawnPointParent = chunk.transform.GetChild(1);
        int spawnCount = spawnPointParent.childCount;

        if (spawnCount == 0)
            return Vector3.zero;

        Transform spawnPoint = spawnPointParent.GetChild((int)playerIndex % spawnCount);
        return spawnPoint.position;
    }

}
