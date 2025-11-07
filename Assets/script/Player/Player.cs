using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Player : NetworkBehaviour
{
    [HideInInspector] public PlayerBaseStatsComponent _playerStats = null;
    public Item _equipiedItem;

    #region Network Data
    public void PlayerSpawned()
    {
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _playerStats = new PlayerBaseStatsComponent(new HealthComponent(), new SpeedComponent(/*maxSpeed : */ 3.5f), new StressComponent());

    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    #endregion

    public void Drop()
    {
        _equipiedItem = null;
        //make drop item logique like just throw on floor
    }

    public void PickupItem(Item item)
    {
        if(_equipiedItem != null) Drop();

        _equipiedItem = item;
    }
}
