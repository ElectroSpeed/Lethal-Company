using System.Collections.Generic;
using UnityEngine;

public abstract class MazeChunk : MonoBehaviour
{
    [Header("Chunk Settings")]
    [SerializeField] protected MazeCell _cellPrefab;
    [Min(1)] public int _width = 10;
    [Min(1)] public int _height = 10;
    [Min(1)] public int _cellSize = 5;

    public int _seed;
    public System.Random _rng;

    private Bounds _bounds;

    [HideInInspector] public readonly List<MazeCell> _chunkCells = new();
    /*[HideInInspector]*/
    public List<MazeChunk> _neighbordsChunks = new();
    public bool _isGenerated;

    public List<CellPair> _doorPairs = new();
    public abstract void CallGenerateMaze();
    public abstract void RegenerateMaze();

    private void OnValidate()
    {
        if (_width % 2 == 0) _width++;
        if (_height % 2 == 0) _height++;
    }

    public void Initialize(int seed)
    {
        _seed = seed;
        _rng = new System.Random(_seed);
        CallGenerateMaze();
        CalculateBounds();
    }

    public void AddDoorPair(MazeCell localCell, MazeCell neighborCell, WallOrientation orientation)
    {
        _doorPairs.Add(new CellPair
        {
            localCell = localCell,
            neighborCell = neighborCell,
            orientation = orientation
        });
    }

    private void CalculateBounds()
    {
        float sizeX = _width * _cellSize;
        float sizeZ = _height * _cellSize;

        Vector3 center = transform.position + new Vector3(sizeX / 2f, 0, sizeZ / 2f);
        _bounds = new Bounds(center, new Vector3(sizeX, 10f, sizeZ)); 
    }

    public bool Contains(Vector3 worldPos)
    {
        return _bounds.Contains(worldPos);
    }
}