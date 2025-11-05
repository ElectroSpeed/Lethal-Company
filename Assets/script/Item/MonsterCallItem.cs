using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MonsterCallItem : Item
{
    [Header("Monster Call Settings")]
    [SerializeField] private float _cooldownTime = 10f;
    [SerializeField] private float _effectDuration = 5f;
    
    private NetworkVariable<bool> _canInteractNet = new NetworkVariable<bool>(
        true, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private bool LocalCanInteract => _canInteractNet.Value;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        _canInteractNet.OnValueChanged += OnCanInteractChanged;
    }

    private void OnCanInteractChanged(bool oldValue, bool newValue)
    {
        _collider.enabled = newValue;
    }

    public override void Interact(Player player)
    {
        if (!LocalCanInteract)
            return;

        base.Interact(player);

        print($"{_itemName} has been activated, beware, the monster is coming!");
        
        ReactiveTrapServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReactiveTrapServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!_canInteractNet.Value)
            return;
        
        _canInteractNet.Value = false;
        
        StartCoroutine(TrapEffectRoutine());

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator TrapEffectRoutine()
    {
        Debug.Log("Monster is active!");

        yield return new WaitForSeconds(_effectDuration);

        Debug.Log("Monster effect ended.");
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(_cooldownTime);
        
        _canInteractNet.Value = true;

        print($"{_itemName} is now reactivated and can be used again.");
    }
    
}
