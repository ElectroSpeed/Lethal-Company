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
        ReactiveTrapServerRpc(player.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReactiveTrapServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
    {
        if (!_canInteractNet.Value)
            return;

        _canInteractNet.Value = false;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
            return;

        Player activatingPlayer = playerObj.GetComponent<Player>();
        
        EnemyBT[] allEnemies = FindObjectsOfType<EnemyBT>();
        foreach (EnemyBT enemy in allEnemies)
        {
            enemy.ForceChasePlayer(activatingPlayer.transform);
        }
        
        StartCoroutine(TrapEffectRoutine());
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator TrapEffectRoutine()
    {
        yield return new WaitForSeconds(_effectDuration);
        
        EnemyBT[] allEnemies = FindObjectsOfType<EnemyBT>();
        foreach (EnemyBT enemy in allEnemies)
        {
            enemy.StopForcedChase();
        }

        Debug.Log("Tous les ennemis ont arrêté la chasse forcée (fin de l'effet).");
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(_cooldownTime);
        _canInteractNet.Value = true;
    }
}
