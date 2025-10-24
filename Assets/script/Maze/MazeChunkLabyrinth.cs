using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeChunkLabyrinth : MazeChunk
{
    private int _iteration = 0;

    [Header("Maze Generation Settings")]
    [SerializeField] private float _mergeWeight = 0.5f;
    [SerializeField] private float _percentWallDestroyed = 0.15f;

    [Header("Visualization (coroutine)")]
    [SerializeField] private float _stepDelay = 0.05f;

    public override void CallGenerateMaze()
    {
        GenerateGrid(_cellPrefab.gameObject, _width, _height, _size);
        GenerateMazeFusionCoroutine();
    }

    private void GenerateGrid(GameObject cellPrefab, int width, int height, int cellSize)
    {
        if (cellPrefab == null || width <= 0 || height <= 0 || cellSize <= 0)
            return;

        _chunkCells.Clear();
        _iteration = 0;

        Vector3 worldPosition = new Vector3(transform.position.x, 0,  transform.position.z);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3((x * cellSize) + worldPosition.x, 0, (y * cellSize) + worldPosition.z);
                MazeCell newCell = Instantiate(cellPrefab, pos, Quaternion.identity, transform).GetComponent<MazeCell>();
                newCell.Init(_iteration);
                _chunkCells.Add(newCell);
                CheckNeighBords(newCell, x, y);
                _iteration++;
            }
        }
    }

    private void CheckNeighBords(MazeCell cell, int x, int y)
    {
        if (x > 0)
        {
            MazeCell neighborX = _chunkCells[(x - 1) * _height + y];
            cell._neighbordsCells.Add(neighborX);
            neighborX._neighbordsCells.Add(cell);
        }
        if (y > 0)
        {
            MazeCell neighborY = _chunkCells[x * _height + (y - 1)];
            cell._neighbordsCells.Add(neighborY);
            neighborY._neighbordsCells.Add(cell);
        }
    }
    
    private void GenerateMazeFusionCoroutine()
    {
        List<MazeCell> visited = new List<MazeCell>();
        Stack<MazeCell> stack = new Stack<MazeCell>();

        MazeCell startCell = _chunkCells[Random.Range(0, _chunkCells.Count)];
        startCell._visited = true;

        startCell.ChangeColor();
        visited.Add(startCell);
        stack.Push(startCell);

        while (stack.Count > 0)
        {
            MazeCell current = stack.Pop();
            List<MazeCell> neighbors = new List<MazeCell>();

            foreach (var n in current._neighbordsCells)
            {
                if (!n._visited)
                    neighbors.Add(n);
            }

            if (neighbors.Count > 0)
            {
                stack.Push(current);

                MazeCell next = GetWeightedNeighbor(neighbors);
                
                next._cellNumber = current._cellNumber;
                next._cellColor = current._cellColor;
                next.ChangeColor();
                
                DestroyWallWithOrientation(current, next);
                
                next._visited = true;
                visited.Add(next);
                stack.Push(next);
            }
        }
        
        OnGenerationCompleted();
    }

    private MazeCell GetWeightedNeighbor(List<MazeCell> neighbors)
    {
        if (neighbors.Count == 1)
            return neighbors[0];

        float totalWeight = neighbors.Count * _mergeWeight;
        float pick = Random.Range(0f, totalWeight);

        int index = Mathf.FloorToInt(pick / _mergeWeight);
        return neighbors[Mathf.Clamp(index, 0, neighbors.Count - 1)];
    }

    private void DestroyWallWithOrientation(MazeCell current, MazeCell next)
    {
        int currentIndex = _chunkCells.IndexOf(current);
        int nextIndex = _chunkCells.IndexOf(next);

        if (currentIndex + _height == nextIndex)
        {
            current.DestroyWall(WallOrientation.Right);
            next.DestroyWall(WallOrientation.Left);
        }
        else if (currentIndex - _height == nextIndex)
        {
            current.DestroyWall(WallOrientation.Left);
            next.DestroyWall(WallOrientation.Right);
        }
        else if (currentIndex + 1 == nextIndex)
        {
            current.DestroyWall(WallOrientation.Up);
            next.DestroyWall(WallOrientation.Down);
        }
        else if (currentIndex - 1 == nextIndex)
        {
            current.DestroyWall(WallOrientation.Down);
            next.DestroyWall(WallOrientation.Up);
        }
    }

    
private void OnGenerationCompleted()
{
    DestroyRandomInternalWalls(_percentWallDestroyed);
}

private void DestroyRandomInternalWalls(float percentage)
{
    int totalCells = _chunkCells.Count;
    int wallsToDestroy = Mathf.RoundToInt(totalCells * percentage);

    int width = _width;
    int height = _height;
    int destroyed = 0;

    while (destroyed < wallsToDestroy)
    {
        MazeCell cell = _chunkCells[Random.Range(0, totalCells)];
        
        int index = _chunkCells.IndexOf(cell);
        int x = index / height;
        int y = index % height;
        
        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            continue;
        
        WallOrientation randomDir = (WallOrientation)Random.Range(0, 4);
        MazeCell neighbor = null;

        switch (randomDir)
        {
            case WallOrientation.Right:
                neighbor = _chunkCells[(x + 1) * height + y];
                break;
            case WallOrientation.Left:
                neighbor = _chunkCells[(x - 1) * height + y];
                break;
            case WallOrientation.Up:
                neighbor = _chunkCells[x * height + (y + 1)];
                break;
            case WallOrientation.Down:
                neighbor = _chunkCells[x * height + (y - 1)];
                break;
        }
        
        if (neighbor == null)
            continue;
        
        cell.DestroyWall(randomDir);
        neighbor.DestroyWall(GetOppositeWall(randomDir));

        destroyed++;
    }
}

private WallOrientation GetOppositeWall(WallOrientation wall)
{
    switch (wall)
    {
        case WallOrientation.Right: return WallOrientation.Left;
        case WallOrientation.Left: return WallOrientation.Right;
        case WallOrientation.Up: return WallOrientation.Down;
        case WallOrientation.Down: return WallOrientation.Up;
        default: return wall;
    }
}

}
