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
    [HideInInspector] public readonly List<MazeCell> _chunkExits = new();

    protected virtual void Start()
    {
        if (_cellPrefab == null)
        {
            return;
        }

        CallGenerateMaze();
    }

    public abstract void CallGenerateMaze();
}