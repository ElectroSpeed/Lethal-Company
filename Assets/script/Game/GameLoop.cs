using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameLoop : NetworkBehaviour
{
    [SerializeField] private float _timeGameDuration = 30f;

    public NetworkVariable<float> _elapsedLifeTime = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    public NetworkVariable<int> _flowerOnMap = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<int> _flowerCollected = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _timeIsOver = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public List<Player> _players = new();

    private void OnEnable()
    {
        _elapsedLifeTime.OnValueChanged += ChangeElapseTimeValue;
        EventBus.Subscribe<Item>(EventType.PickFlower, OnFlowerPicked);
    }

    private void OnDisable()
    {
        _elapsedLifeTime.OnValueChanged -= ChangeElapseTimeValue;
        EventBus.Unsubscribe<Item>(EventType.PickFlower, OnFlowerPicked);
    }

    private void OnFlowerPicked(Item item)
    {
        if (!IsServer) return;

        _flowerCollected.Value++;

        Debug.Log($"Flower collected: {_flowerCollected.Value}/{_flowerOnMap.Value}");
        
        if (IsGameWin())
        {
            GameWin();
        }
    }


    private void ChangeElapseTimeValue(float oldValue, float newValue)
    {
        OnElapsedTimeChanged?.Invoke(newValue);
    }

    public static event Action<float> OnElapsedTimeChanged;
    
    [SerializeField] private MapManager _mapManager;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _elapsedLifeTime.Value = _timeGameDuration;
        }
    }

    private void Start()
    {
        if (!IsServer) return;

        if (_mapManager != null)
        {
            _flowerOnMap.Value = _mapManager._objectiveFlowerItemMaxCount; 
        }
    }

    void Update()
    {
        if (!IsServer) return;
        if (_timeIsOver.Value) return;

        _elapsedLifeTime.Value -= Time.deltaTime;

        if (_elapsedLifeTime.Value <= 0f)
        {
            _elapsedLifeTime.Value = 0f;
            _timeIsOver.Value = true;
            GameOver();
        }
    }

    private int CheckCountPlayerAlive()
    {
        int count = 0;
        foreach (var player in _players)
        {
            var health = player._playerStats._healthComponent;
            if (health._isAlive) count++;
        }
        return count;
    }

    public bool IsGameWin()
    {
        return _flowerCollected.Value == _flowerOnMap.Value;
    }

    public bool IsGameOver()
    {
        return _timeIsOver.Value || CheckCountPlayerAlive() == 0;
    }

    private void GameWin()
    {
        Debug.Log("Game Win (server)");
        
        GameOverClientRpc();

        if (IsServer)
        {
            StartCoroutine(ReturnToMainMenuAfterDelay(3f));
        }
    }

    private void GameOver()
    {
        if (!IsGameOver()) return;

        Debug.Log("Game Over (server)");
        GameOverClientRpc();

        if (IsServer)
        {
            StartCoroutine(ReturnToMainMenuAfterDelay(3f));
        }
    }


    private IEnumerator ReturnToMainMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        NetworkManager.Singleton.SceneManager.LoadScene(
            "MainMenus",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }


    
    [ClientRpc]
    private void GameOverClientRpc()
    {
        Debug.Log("Game Over reçu côté client !");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}