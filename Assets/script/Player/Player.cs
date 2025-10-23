using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Player : NetworkBehaviour
{
    [HideInInspector] public PlayerBaseStatsComponent _playerStats = null;

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
}
