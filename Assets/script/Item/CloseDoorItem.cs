using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CloseDoorItem : Item
{
    [Header("Close Door Settings")]
    [SerializeField] private float _cooldownTime = 10f;
    [SerializeField] private float _effectDuration = 20f;

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
        ActivateTrapServerRpc(player.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateTrapServerRpc(ulong playerId)
    {
        if (!_canInteractNet.Value)
            return;

        _canInteractNet.Value = false;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
            return;

        Player activatingPlayer = playerObj.GetComponent<Player>();
        MapManager map = FindObjectOfType<MapManager>();

        if (map == null)
            return;

        MazeChunk playerChunk = map.GetChunkFromWorldPosition(activatingPlayer.transform.position);

        if (playerChunk == null)
            return;

        StartCoroutine(CloseChunkRoutine(map, playerChunk));
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CloseChunkRoutine(MapManager map, MazeChunk chunk)
    {
        map.SetChunkDoorsStateClientRpc(map.GetChunkIndex(chunk), false);

        yield return new WaitForSeconds(_effectDuration);
        
        map.SetChunkDoorsStateClientRpc(map.GetChunkIndex(chunk), true);
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(_cooldownTime);
        _canInteractNet.Value = true;
    }
}
