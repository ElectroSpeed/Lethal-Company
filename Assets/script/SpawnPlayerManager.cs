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

        _mapGenerated = value;

        if (_mapGenerated && IsServer)
        {
            StartCoroutine(RepositionAllPlayers());
        }
    }

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
        StartCoroutine(SpawnPlayerPrefab(clientId));
        StartCoroutine(_mapManager.WaitForPlayerConnected());
    }

    private IEnumerator SpawnPlayerPrefab(ulong clientId)
    {
        yield return new WaitForEndOfFrame();

        GameObject player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);

        var netObj = player.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            netObj = player.AddComponent<NetworkObject>();
        }

        netObj.SpawnAsPlayerObject(clientId);

        if (_mapGenerated)
        {
            MovePlayerToMapPosition(player);
        }
        else
        {
            player.SetActive(false);
        }

        EventBus.Publish(EventType.AllPlayerWasConnected, true);
    }

    private IEnumerator RepositionAllPlayers()
    {
        yield return new WaitForSeconds(0.5f);

        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObj = kvp.Value.PlayerObject;

            if (playerObj == null)
            {
                continue;
            }

            MovePlayerToMapPosition(playerObj.gameObject);
            playerObj.gameObject.SetActive(true);
        }

    }

    private void MovePlayerToMapPosition(GameObject player)
    {
        if (_mapManager == null || _mapManager._safeChunk == null)
        {
            return;
        }

        var chunk = _mapManager._safeChunk;


        float minX = chunk.transform.position.x - chunk._width / 2f;
        float maxX = chunk.transform.position.x + chunk._width / 2f;
        float minZ = chunk.transform.position.z - chunk._height / 2f;
        float maxZ = chunk.transform.position.z + chunk._height / 2f;

        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 playerPos = new Vector3(randomX, 1.5f, randomZ);

        player.transform.position = playerPos;
    }
}
