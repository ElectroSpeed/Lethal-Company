using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _playerNameText;
    [SerializeField] private TextMeshProUGUI _readyStateText;
    [SerializeField] private Button _readyButton;

    public NetworkVariable<bool> IsReady = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> PlayerName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string playerName = PlayerPrefs.GetString("PlayerName", $"Player_{OwnerClientId}");
            SetPlayerNameServerRpc(playerName);

            _readyButton.gameObject.SetActive(true);
            _readyButton.onClick.AddListener(OnReadyClicked);
        }

        PlayerName.OnValueChanged += OnPlayerNameChanged;
        IsReady.OnValueChanged += OnReadyStateChanged;

        _playerNameText.text = PlayerName.Value.ToString();
        _readyStateText.text = IsReady.Value ? "Ready" : "Not Ready";
        _readyStateText.color = IsReady.Value ? Color.green : Color.red;
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(string name)
    {
        PlayerName.Value = name;
    }

    private void OnReadyClicked()
    {
        ToggleReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = true)]
    private void ToggleReadyServerRpc()
    {
        IsReady.Value = !IsReady.Value;
        LobbyManager.Instance.CheckAllReady();
    }

    private void OnPlayerNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        _playerNameText.text = newValue.ToString();
    }

    private void OnReadyStateChanged(bool oldValue, bool newValue)
    {
        _readyStateText.text = newValue ? "Ready" : "Not Ready";
        _readyStateText.color = newValue ? Color.green : Color.red;
    }
}
