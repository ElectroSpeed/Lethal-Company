using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpawnPlayerManager : NetworkBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private MapManager _mapManager;

    private bool _mapGenerated;

    private void Awake()
    {
        if (_mapManager == null)
            _mapManager = GetComponent<MapManager>();

        Debug.Log("[SpawnPlayerManager] Awake - MapManager assigned: " + (_mapManager != null));
    }

    private void OnEnable()
    {
        EventBus.Subscribe<bool>(EventType.MapGenerated, OnMapGenerated);
        Debug.Log("[SpawnPlayerManager] Subscribed to MapGenerated event");
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<bool>(EventType.MapGenerated, OnMapGenerated);
        Debug.Log("[SpawnPlayerManager] Unsubscribed from MapGenerated event");
    }

    private void OnMapGenerated(bool value)
    {
        Debug.Log($"[SpawnPlayerManager] Event OnMapGenerated received: {value}");

        _mapGenerated = value;

        if (_mapGenerated && IsServer)
        {
            Debug.Log("[SpawnPlayerManager] Map generated - repositioning all players...");
            StartCoroutine(RepositionAllPlayers());
        }
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[SpawnPlayerManager] OnNetworkSpawn called on {(IsServer ? "Server" : "Client")}");

        if (!IsServer) return;

        Debug.Log("[SpawnPlayerManager] Registering OnClientConnectedCallback");
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Debug.Log($"[SpawnPlayerManager] Existing client detected on spawn: {clientId}");
            OnClientConnected(clientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[SpawnPlayerManager] OnClientConnected called for client ID :  {clientId}");

        StartCoroutine(SpawnPlayerPrefab(clientId));


        Debug.Log($"-------[SpawnPlayerManager] Call WaitForPlayerConnected on OnClientConnected-------------");
        StartCoroutine(_mapManager.WaitForPlayerConnected());
    }

    private IEnumerator SpawnPlayerPrefab(ulong clientId)
    {
        Debug.Log($"[SpawnPlayerManager] Starting coroutine SpawnPlayerPrefab for client {clientId}");
        yield return new WaitForEndOfFrame();

        Debug.Log($"[SpawnPlayerManager] Instantiating player prefab for client {clientId}");
        GameObject player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);

        var netObj = player.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogWarning($"[SpawnPlayerManager] Player prefab had no NetworkObject, adding one dynamically.");
            netObj = player.AddComponent<NetworkObject>();
        }

        Debug.Log($"[SpawnPlayerManager] Spawning NetworkObject for player (client {clientId})");
        netObj.SpawnAsPlayerObject(clientId);

        if (_mapGenerated)
        {
            Debug.Log($"[SpawnPlayerManager] Map already generated - moving player {clientId} immediately.");
            MovePlayerToMapPosition(player);
        }
        else
        {
            Debug.Log($"[SpawnPlayerManager] Map not ready yet - disabling player {clientId}.");
            player.SetActive(false);
        }


        Debug.Log($"[SpawnPlayerManager] Send Event 'AllPlayerWasConnected' to Map Manager");
        EventBus.Publish(EventType.AllPlayerWasConnected, true);
    }

    private IEnumerator RepositionAllPlayers()
    {
        Debug.Log("[SpawnPlayerManager] Waiting 0.5s before repositioning all players...");
        yield return new WaitForSeconds(0.5f);

        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObj = kvp.Value.PlayerObject;

            if (playerObj == null)
            {
                Debug.LogWarning($"[SpawnPlayerManager] Client {kvp.Key} has no PlayerObject yet!");
                continue;
            }

            Debug.Log($"[SpawnPlayerManager] Repositioning player {kvp.Key} to safe chunk...");
            MovePlayerToMapPosition(playerObj.gameObject);
            playerObj.gameObject.SetActive(true);
        }

        Debug.Log("[SpawnPlayerManager] All players repositioned and activated.");
    }

    private void MovePlayerToMapPosition(GameObject player)
    {
        if (_mapManager == null || _mapManager._safeChunk == null)
        {
            Debug.LogWarning("[SpawnPlayerManager] Cannot move player - MapManager or SafeChunk is null.");
            return;
        }

        var chunk = _mapManager._safeChunk;

        Debug.Log($"[SpawnPlayerManager] Moving player to chunk: {chunk.name}");

        float minX = chunk.transform.position.x - chunk._width / 2f;
        float maxX = chunk.transform.position.x + chunk._width / 2f;
        float minZ = chunk.transform.position.z - chunk._height / 2f;
        float maxZ = chunk.transform.position.z + chunk._height / 2f;

        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 playerPos = new Vector3(randomX, 1.5f, randomZ);

        Debug.Log($"[SpawnPlayerManager] Player moved to position: {playerPos}");
        player.transform.position = playerPos;
    }
}
