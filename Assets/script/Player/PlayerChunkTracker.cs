using UnityEngine;

public class PlayerChunkTracker : MonoBehaviour
{
    [SerializeField] private MapManager _mapManager;
    public MazeChunk _currentChunk;
    [SerializeField, Min(1)] private int _regenRange = 1;

    private void Start()
    {
        if (_mapManager == null)
        {
            _mapManager = FindObjectOfType<MapManager>();
        }
        UpdateCurrentChunk();
    }

    private void Update()
    {
        if (_mapManager == null) return;

        MazeChunk chunk = _mapManager.GetChunkFromPosition(transform.position);
        if (chunk != null && chunk != _currentChunk)
        {
            _currentChunk = chunk;
            _mapManager.RegenerateChunksAroundPlayer(_currentChunk, _regenRange);
        }

    }

    private void UpdateCurrentChunk()
    {
        if (_mapManager == null) return;
        _currentChunk = _mapManager.GetChunkFromPosition(transform.position);
    }
}