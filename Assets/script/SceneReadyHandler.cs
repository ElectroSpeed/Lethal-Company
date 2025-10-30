using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReadyHandler : NetworkBehaviour
{
    [SerializeField] private MapManager _mapManager;
    private void Start()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        base.OnNetworkDespawn();

        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        if (clientsTimedOut.Count > 0)
        {
            Debug.LogWarning("Certains clients ont expiré pendant le chargement de la scène.");
            return;
        }

        if (_mapManager != null)
        {
            _mapManager.StartMapGeneration();
        }
    }
}
