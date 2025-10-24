using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform _playerListParent;
    [SerializeField] private GameObject _playerSlotPrefab;
    [SerializeField] private GameObject _startButton;

    private readonly List<LobbyPlayer> _connectedPlayers = new();

    public static LobbyManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        _startButton.SetActive(IsServer);
    }

    private void OnClientConnected(ulong clientId)
    {
        // Création du joueur côté serveur
        var playerObj = Instantiate(_playerSlotPrefab, _playerListParent);
        var lobbyPlayer = playerObj.GetComponent<LobbyPlayer>();
        lobbyPlayer.NetworkObject.SpawnAsPlayerObject(clientId);
        _connectedPlayers.Add(lobbyPlayer);
    }

    public void CheckAllReady()
    {
        if (!IsServer) return;

        bool allReady = true;
        foreach (var player in _connectedPlayers)
        {
            if (!player.IsReady.Value)
            {
                allReady = false;
                break;
            }
        }

        _startButton.SetActive(allReady);
    }

    public void StartGame()
    {
        if (!IsServer) return;
        SceneManager.LoadScene("GameScene"); // ou via NetworkSceneManager si tu veux synchroniser
    }
}
