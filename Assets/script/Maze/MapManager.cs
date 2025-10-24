using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private MazeChunk _chunkLabyrinthPrefab;
    [SerializeField] private MazeChunk _chunkSafePrefab;

    [SerializeField, Min(1)] private int _width = 5;
    [SerializeField, Min(1)] private int _height = 5;
    [SerializeField] private Vector2Int _chunkSize = new Vector2Int(50, 50);

    private readonly List<MazeChunk> _mapChunks = new();

    private void OnValidate()
    {
        if (_width % 2 == 0) _width++;
        if (_height % 2 == 0) _height++;
        
    }


    private void Start()
    {
        GenerateChunkGrid();
    }

    private void GenerateChunkGrid()
    {
        if (!_chunkLabyrinthPrefab || !_chunkSafePrefab)
        {
            return;
        }

        _mapChunks.Clear();

        Vector3 startOffset = new Vector3(
            -(_width / 2f) * _chunkSize.x,
            0,
            -(_height / 2f) * _chunkSize.y
        );

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector3 pos = new Vector3(x * _chunkSize.x, 0, y * _chunkSize.y) + startOffset;
                bool isCenter = (x == _width / 2 && y == _height / 2);

                MazeChunk prefab = isCenter ? _chunkSafePrefab : _chunkLabyrinthPrefab;
                MazeChunk newChunk = Instantiate(prefab, pos, Quaternion.identity, transform);
                _mapChunks.Add(newChunk);
            }
        }
    }
}