using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerLobby : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _readyText;
    [SerializeField] private GameObject _isReadyButton;

    private NetworkVariable<FixedString32Bytes> _playerName = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _isReady = new(writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        _playerName.OnValueChanged += OnNameChanged;
        _isReady.OnValueChanged += OnReadyChanged;

        if (IsServer)
        {
            _isReady.Value = true;
            _isReadyButton.SetActive(false);
            OnReadyChanged(default, true);
        }
        else if (IsClient && !IsHost)
        {
            _isReadyButton.SetActive(true);

           // SetClientNotReadyServerRpc();
        }

        if (IsOwner)
        {
            string playerName = PlayerPrefs.GetString("PlayerName", $"Player_{OwnerClientId}");
            SubmitNameServerRpc(playerName);
        }

        OnNameChanged(default, _playerName.Value);
        OnReadyChanged(default, _isReady.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetClientNotReadyServerRpc()
    {
        _isReady.Value = false;
    }

    private void OnNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        if (_nameText != null)
            _nameText.text = newValue.ToString();
    }

    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        if (_readyText == null) return;

        _readyText.text = newValue ? "Ready" : "Not Ready";
        _readyText.color = newValue ? Color.green : Color.red;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNameServerRpc(string playerName)
    {
        _playerName.Value = playerName;
    }

    public void ToggleReady()
    {
        if (IsOwner)
            ToggleReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc()
    {
        _isReady.Value = !_isReady.Value;
        LobbyManager.Instance.CheckAllPlayersReady();
    }

    public bool IsReady()
    {
        return _isReady.Value;
    }
}
