using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class MainMenusManager : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private string _gameSceneName = "GameScene";


    [Header("DEBUG")]
    [SerializeField] private string _MultiplayerScene = "Multiplayer";

    private void Awake()
    {
        if (_networkManager == null)
            _networkManager = NetworkManager.Singleton;
    }

    public void HostGame()
    {
        _networkManager.StartHost();

        if (_networkManager.IsServer)
        {
            _networkManager.SceneManager.LoadScene(_MultiplayerScene, LoadSceneMode.Single);
        }
    }

    public void JoinGame()
    {
        _networkManager.StartClient();
        if (_networkManager.IsServer)
        {
            _networkManager.SceneManager.LoadScene(_MultiplayerScene, LoadSceneMode.Single);
        }
    }

    public void JoinGameWithIP(string ipAddress)
    {
        var transport = (UnityTransport)_networkManager.NetworkConfig.NetworkTransport;
        transport.SetConnectionData(ipAddress, 7777);
        _networkManager.StartClient();
    }
}
