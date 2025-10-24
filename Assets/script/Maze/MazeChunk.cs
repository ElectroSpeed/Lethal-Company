using System.Collections.Generic;
using UnityEngine;

public abstract class MazeChunk : MonoBehaviour
{
    [Header("Chunk Settings")]
    [SerializeField] protected MazeCell _cellPrefab;
    [SerializeField] protected int _width;
    [SerializeField] protected int _height;
    [SerializeField] protected int _size;

    [HideInInspector] public List<MazeCell> _chunkCells = new List<MazeCell>();
    [HideInInspector] public List<MazeCell> _chunkExits = new List<MazeCell>();

    protected virtual void Start()
    {
        CallGenerateMaze();
    }

    public abstract void CallGenerateMaze();
}