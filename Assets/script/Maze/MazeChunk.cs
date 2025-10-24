using System.Collections.Generic;
using UnityEngine;

public abstract class MazeChunk : MonoBehaviour
{
    [Header("Chunk Settings")]
    [SerializeField] protected MazeCell _cellPrefab;
    [SerializeField, Min(1)] protected int _width = 10;
    [SerializeField, Min(1)] protected int _height = 10;
    [SerializeField, Min(1)] protected int _size = 5;

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