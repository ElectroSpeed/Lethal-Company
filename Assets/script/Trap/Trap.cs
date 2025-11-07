using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public abstract class Trap : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] protected BoxCollider _triggerCollider;
    [SerializeField, Min(0.1f)] protected float _trapDuration = 2f;
    [SerializeField, Min(0.1f)] protected float _rearmedTrapTime = 3f;

    [Header("Internal State (Network Synced)")]
    protected NetworkVariable<TrapState> _state = new NetworkVariable<TrapState>(
        TrapState.Armed,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    protected Coroutine _trapCoroutine;


    public override void OnNetworkSpawn()
    {
        Initialize();
    }
    protected virtual void Initialize()
    {
        if (_triggerCollider == null)
            _triggerCollider = GetComponent<BoxCollider>();

        _triggerCollider.isTrigger = true;

    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (_state.Value != TrapState.Armed) return;

        if (other.TryGetComponent(out Player player))
        {
            PlayerTriggeredServer(player);
        }
    }

    protected virtual void PlayerTriggeredServer(Player player)
    {
        _trapCoroutine = StartCoroutine(ActivateTrapRoutine(player));
    }

    private IEnumerator ActivateTrapRoutine(Player player)
    {
        _state.Value = TrapState.Activated;

        OnTrapActivatedServer(player);
        NotifyTrapActivatedClientRpc();

        yield return new WaitForSeconds(_trapDuration);

        OnTrapEffectFinishedServer(player);
        _state.Value = TrapState.WaitingForRearmed;
        NotifyTrapEffectFinishedClientRpc();

        yield return new WaitForSeconds(_rearmedTrapTime);

        _state.Value = TrapState.Armed;
        OnTrapRearmedServer();
        NotifyTrapRearmedClientRpc();
    }

    // --- SERVER SIDE LOGIC (à surcharger) ---
    protected abstract void OnTrapActivatedServer(Player player);
    protected virtual void OnTrapEffectFinishedServer(Player player) { }
    protected virtual void OnTrapRearmedServer() { }

    // --- CLIENT SIDE VISUALS (RPCs) ---
    [ClientRpc]
    protected void NotifyTrapActivatedClientRpc()
    {
        OnTrapActivatedClient();
    }

    [ClientRpc]
    protected void NotifyTrapEffectFinishedClientRpc()
    {
        OnTrapEffectFinishedClient();
    }

    [ClientRpc]
    protected void NotifyTrapRearmedClientRpc()
    {
        OnTrapRearmedClient();
    }

    // --- CLIENT VISUAL CALLBACKS (à surcharger si besoin) ---
    protected virtual void OnTrapActivatedClient() { }
    protected virtual void OnTrapEffectFinishedClient() { }
    protected virtual void OnTrapRearmedClient() { }
}

public enum TrapState
{
    None,
    Armed,
    Activated,
    WaitingForRearmed
}
