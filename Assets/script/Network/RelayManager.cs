using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _joinCodeText;
    [SerializeField] private TMP_InputField _joinCodeInputField;

    [Header("Save Player Pseudo")]
    [SerializeField] private TMP_InputField _joinPlayerSpeudo;
    [SerializeField] private TMP_InputField _hostPlayerSpeudo;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();


    }

    public async void StartRelay()
    {
        string joinCode = await StartHostWithRelay(4);
        PlayerPrefs.SetString("JoinCode", joinCode);
        _joinCodeText.text = joinCode;

    }

    public void ChangeScene()
    {
        string pseudo = _hostPlayerSpeudo.text;
        if (string.IsNullOrEmpty(pseudo))
        {
            pseudo = "Host_ " + Random.Range(1000, 9999);
        }
        PlayerPrefs.SetString("PlayerName", pseudo);

        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
    }
    public void StopRelayAndReturnToMenu()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Stopping Host/Server...");
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Stopping Client...");
        }

        NetworkManager.Singleton.Shutdown(); 
    }



    public async void JoinRelay()
    {
        await StartClienWithRelay(_joinCodeInputField.text);

        string pseudo = _joinPlayerSpeudo.text;
        if (string.IsNullOrEmpty(pseudo))
        {
            pseudo = "client_ " + Random.Range(1000, 9999);
        }
        PlayerPrefs.SetString("PlayerName", pseudo);

        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
    }

    private async Task<string> StartHostWithRelay(int maxConnection = 4)
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnection);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        print("join code is : " + joinCode);
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }

    private async Task<bool> StartClienWithRelay(string joinCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
    }
}