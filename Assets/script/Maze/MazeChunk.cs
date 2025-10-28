using System.Collections.Generic;
using UnityEngine;

public abstract class MazeChunk : MonoBehaviour
{
    [Header("Chunk Settings")]
    [SerializeField] protected MazeCell _cellPrefab;
    [Min(1)] public int _width = 10;
    [Min(1)] public int _height = 10;
    [Min(1)] public int _size = 5;

    [HideInInspector] public readonly List<MazeCell> _chunkCells = new();
    [HideInInspector] public readonly List<MazeChunk> _neighborChunks = new();
    [HideInInspector] public List<MazeChunk> _neighbordsChunks = new();
    public bool _isGenerated;

    public List<CellPair> _doorPairs = new();
    public abstract void CallGenerateMaze();
    public abstract void RegenerateMaze();

    protected virtual void Start()
    {
        if (_cellPrefab == null)
            return;

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

    public void CloseWallAt()
    {

    }


}