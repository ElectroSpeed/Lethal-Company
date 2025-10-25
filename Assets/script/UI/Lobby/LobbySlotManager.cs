using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.LowLevel;

public class LobbySlotManager : NetworkBehaviour
{
    public static LobbySlotManager Instance;

    [SerializeField] private Transform[] _slots;
    [SerializeField] private GameObject _playerLobbyPrefab;

    private Dictionary<ulong, int> _playerSlotIndex = new Dictionary<ulong, int>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!_playerSlotIndex.ContainsKey(clientId))
            {
                OnClientConnected(clientId);
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        int slotIndex = GetFirstFreeSlot();
        if (slotIndex == -1)
        {
            Debug.LogWarning("Aucun slot disponible !");
            return;
        }

        GameObject playerLobby = Instantiate(_playerLobbyPrefab, _slots[slotIndex].position + new Vector3(0, 1.5f, 0), _slots[slotIndex].rotation);

        var netObj = playerLobby.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);

        _playerSlotIndex[clientId] = slotIndex;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (_playerSlotIndex.ContainsKey(clientId))
        {
            _playerSlotIndex.Remove(clientId);
        }
    }

    private int GetFirstFreeSlot()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_playerSlotIndex.ContainsValue(i))
                return i;
        }
        return -1;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
