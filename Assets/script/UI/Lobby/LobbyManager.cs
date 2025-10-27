using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject _startGameButton;
    [SerializeField] private TextMeshProUGUI _hostjoinCode;

    [Header("Players")]
    [SerializeField] public List<PlayerLobby> _playerPrefabs = new();

    private void Awake()
    {
        Instance = this;
        _startGameButton.SetActive(false);

        _hostjoinCode.text = PlayerPrefs.GetString("JoinCode", "ERROR");
        _hostjoinCode.color = Color.red;


        if (/*clients.Count == 1 && */NetworkManager.Singleton.IsHost)
        {
            _startGameButton.SetActive(true);
            return;
        }
    }

    public void CheckAllPlayersReady()
    {
        if (!IsServer) return;

        var clients = NetworkManager.Singleton.ConnectedClientsList;

        bool allReady = true;

        foreach (var client in clients)
        {
            PlayerLobby playerLobby = client.PlayerObject.GetComponent<PlayerLobby>();
            if (playerLobby == null || !playerLobby.IsReady())
            {
                allReady = false;
                break;
            }
        }

        if (NetworkManager.Singleton.IsHost)
        {
            _startGameButton.SetActive(allReady);
        }
    }

    public void StartGame()
    {
        if (!IsServer) return;

        foreach (var lobbyPlayer in _playerPrefabs)
        {
            if (lobbyPlayer != null && lobbyPlayer.NetworkObject != null && lobbyPlayer.NetworkObject.IsSpawned)
            {
                lobbyPlayer.NetworkObject.Despawn(true);
            }
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            LoadSceneMode.Single
        );
    }
}
