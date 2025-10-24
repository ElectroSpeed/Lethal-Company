using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeChunkLabyrinth : MazeChunk
{
    [Header("Maze Generation Settings")]
    [SerializeField, Range(0f, 1f)] private float _mergeWeight = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _percentWallDestroyed = 0.15f;

    [Header("Visualization")]
    [SerializeField] private float _stepDelay = 0.05f;

    private int _iteration;

    public override void CallGenerateMaze()
    {
        GenerateGrid(_cellPrefab.gameObject, _width, _height, _size);
        GenerateMazeFusion();
    }

    private void GenerateGrid(GameObject cellPrefab, int width, int height, int cellSize)
    {
        if (cellPrefab == null || width <= 0 || height <= 0 || cellSize <= 0)
            return;

        _chunkCells.Clear();
        _iteration = 0;

        Vector3 worldOffset = transform.position;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize) + worldOffset;
                MazeCell newCell = Instantiate(cellPrefab, pos, Quaternion.identity, transform).GetComponent<MazeCell>();
                newCell.Init(_iteration);
                _chunkCells.Add(newCell);
                
                if (x > 0)
                {
                    MazeCell left = _chunkCells[(x - 1) * height + y];
                    newCell._neighbordsCells.Add(left);
                    left._neighbordsCells.Add(newCell);
                }
                if (y > 0)
                {
                    MazeCell down = _chunkCells[x * height + (y - 1)];
                    newCell._neighbordsCells.Add(down);
                    down._neighbordsCells.Add(newCell);
                }

                _iteration++;
            }
        }
    }

    private void GenerateMazeFusion()
    {
        List<MazeCell> visited = new();
        Stack<MazeCell> stack = new();

        MazeCell start = _chunkCells[Random.Range(0, _chunkCells.Count)];
        start._visited = true;
        start.ChangeColor();
        visited.Add(start);
        stack.Push(start);

        while (stack.Count > 0)
        {
            MazeCell current = stack.Pop();
            List<MazeCell> neighbors = new();

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

        int cx = currentIndex / _height;
        int cy = currentIndex % _height;
        int nx = nextIndex / _height;
        int ny = nextIndex % _height;

        if (nx == cx + 1) { current.DestroyWall(WallOrientation.Right); next.DestroyWall(WallOrientation.Left); }
        else if (nx == cx - 1) { current.DestroyWall(WallOrientation.Left); next.DestroyWall(WallOrientation.Right); }
        else if (ny == cy + 1) { current.DestroyWall(WallOrientation.Up); next.DestroyWall(WallOrientation.Down); }
        else if (ny == cy - 1) { current.DestroyWall(WallOrientation.Down); next.DestroyWall(WallOrientation.Up); }
    }

    private void OnGenerationCompleted()
    {
        DestroyRandomInternalWalls(_percentWallDestroyed);
    }

    private void DestroyRandomInternalWalls(float percentage)
    {
        int totalCells = _chunkCells.Count;
        int wallsToDestroy = Mathf.RoundToInt(totalCells * percentage);
        int destroyed = 0;

        while (destroyed < wallsToDestroy)
        {
            MazeCell cell = _chunkCells[Random.Range(0, totalCells)];

            int index = _chunkCells.IndexOf(cell);
            int x = index / _height;
            int y = index % _height;

            if (x == 0 || y == 0 || x == _width - 1 || y == _height - 1)
                continue;

            WallOrientation dir = (WallOrientation)Random.Range(0, 4);
            MazeCell neighbor = GetNeighbor(x, y, dir);

            if (neighbor == null)
                continue;

            cell.DestroyWall(dir);
            neighbor.DestroyWall(GetOppositeWall(dir));

            destroyed++;
        }
    }

    private MazeCell GetNeighbor(int x, int y, WallOrientation dir)
    {
        return dir switch
        {
            WallOrientation.Right => (x + 1 < _width) ? _chunkCells[(x + 1) * _height + y] : null,
            WallOrientation.Left => (x - 1 >= 0) ? _chunkCells[(x - 1) * _height + y] : null,
            WallOrientation.Up => (y + 1 < _height) ? _chunkCells[x * _height + (y + 1)] : null,
            WallOrientation.Down => (y - 1 >= 0) ? _chunkCells[x * _height + (y - 1)] : null,
            _ => null
        };
    }

    private WallOrientation GetOppositeWall(WallOrientation wall)
    {
        return wall switch
        {
            WallOrientation.Right => WallOrientation.Left,
            WallOrientation.Left => WallOrientation.Right,
            WallOrientation.Up => WallOrientation.Down,
            WallOrientation.Down => WallOrientation.Up,
            _ => wall
        };
    }
}
