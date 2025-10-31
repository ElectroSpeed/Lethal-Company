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

    [HideInInspector] public readonly List<MazeCell> _chunkCells = new();
    /*[HideInInspector]*/ public List<MazeChunk> _neighbordsChunks = new();
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
}