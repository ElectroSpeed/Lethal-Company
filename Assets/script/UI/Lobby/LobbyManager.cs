using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;
    [SerializeField] private GameObject _startGameButton;
    [SerializeField] private TextMeshProUGUI _hostjoinCode;
    [SerializeField] public List<PlayerLobby> _playerPrefabs = new();

    private void Awake()
    {
        Instance = this;
        _startGameButton.SetActive(false);

        _hostjoinCode.text = PlayerPrefs.GetString("JoinCode", "ERROR");
        _hostjoinCode.color = Color.red;
    }



    public void CheckAllPlayersReady()
    {
        if (!IsServer) return;

        bool allReady = true;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
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
            if (lobbyPlayer.NetworkObject != null && lobbyPlayer.NetworkObject.IsSpawned)
                lobbyPlayer.NetworkObject.Despawn(true);
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            "Multiplayer",
            LoadSceneMode.Single
        );
    }
}