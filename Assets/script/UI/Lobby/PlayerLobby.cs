using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerLobby : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;      // Pseudo
    [SerializeField] private TextMeshProUGUI _readyText;     // Texte Ready

    private NetworkVariable<FixedString32Bytes> _playerName = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _isReady = new(writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // Pseudo synchronisé pour tous
        _playerName.OnValueChanged += (_, newValue) => _nameText.text = newValue.ToString();

        // Ready synchronisé pour tous
        _isReady.OnValueChanged += (_, newValue) =>
        {
            if (_readyText == null) return;

            if (newValue)
            {
                _readyText.text = "Ready";
                _readyText.color = Color.green;
            }
            else
            {
                _readyText.text = "Not Ready";
                _readyText.color = Color.red;
            }
        };

        // Initialisation par défaut
        if (IsServer)
        {
            _isReady.Value = false;
        }

        // Owner définit son pseudo une seule fois
        if (IsOwner && IsClient)
        {
            string playerName = PlayerPrefs.GetString("PlayerName", $"Player_{OwnerClientId}");
            SubmitNameServerRpc(playerName);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNameServerRpc(string playerName)
    {
        _playerName.Value = playerName;
    }

    // Appelé par le bouton Ready
    public void ToggleReady()
    {
        if (IsOwner)
        {
            ToggleReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc()
    {
        _isReady.Value = !_isReady.Value;
        LobbyManager.Instance.CheckAllPlayersReady();
    }

    // Pour que le LobbyManager puisse vérifier l'état
    public bool IsReady() => _isReady.Value;
}
