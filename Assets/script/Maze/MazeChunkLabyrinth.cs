using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeChunkLabyrinth : MazeChunk
{
    private int _iteration = 0;

    [Header("Maze Generation Settings")]
    [SerializeField] private float _mergeWeight = 0.5f;

    [Header("Visualization (coroutine)")]
    [SerializeField] private float _stepDelay = 0.05f;

    public override void CallGenerateMaze()
    {
        GenerateGrid(_cellPrefab.gameObject, _width, _height, _size);
        StopAllCoroutines();
        StartCoroutine(GenerateMazeFusionCoroutine());
    }

    private void GenerateGrid(GameObject cellPrefab, int width, int height, int cellSize)
    {
        if (cellPrefab == null || width <= 0 || height <= 0 || cellSize <= 0)
            return;

        _chunkCells.Clear();
        _iteration = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
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
    
    private IEnumerator GenerateMazeFusionCoroutine()
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
                
                if (_stepDelay <= 0f)
                    yield return null;
                else
                    yield return new WaitForSeconds(_stepDelay);
            }
            else
            {
                if (_stepDelay <= 0f)
                    yield return null;
                else
                    yield return new WaitForSeconds(_stepDelay);
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

        if (currentIndex + _height == nextIndex) // droite
        {
            current.DestroyWall(WallOrientation.Right);
            next.DestroyWall(WallOrientation.Left);
        }
        else if (currentIndex - _height == nextIndex) // gauche
        {
            current.DestroyWall(WallOrientation.Left);
            next.DestroyWall(WallOrientation.Right);
        }
        else if (currentIndex + 1 == nextIndex) // haut (y+1)
        {
            current.DestroyWall(WallOrientation.Up);
            next.DestroyWall(WallOrientation.Down);
        }
        else if (currentIndex - 1 == nextIndex) // bas (y-1)
        {
            current.DestroyWall(WallOrientation.Down);
            next.DestroyWall(WallOrientation.Up);
        }
    }

    
    protected virtual void OnGenerationCompleted()
    {
        Debug.Log("Maze generation (fusion) completed.");
    }
}
