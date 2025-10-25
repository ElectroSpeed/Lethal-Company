using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [SerializeField] private GameObject _startGameButton;

    private void Awake()
    {
        Instance = this;
        _startGameButton.gameObject.SetActive(false);
    }

    public void CheckAllPlayersReady()
    {
        if (!IsServer) return;

        bool allReady = true;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            PlayerLobby playerLobby = client.PlayerObject.GetComponent<PlayerLobby>();
            if (playerLobby != null && !playerLobby.IsReady())
            {
                allReady = false;
                break;
            }
        }

        if (NetworkManager.Singleton.IsHost)
        {
            _startGameButton.gameObject.SetActive(allReady);
        }
    }

    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Multiplayer", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
