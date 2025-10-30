using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class MazeChunkLabyrinth : MazeChunk
{
    [Header("Maze Generation Settings")]
    [SerializeField, Range(0f, 1f)] private float _percentWallDestroyed = 0.15f;
    [SerializeField] private float _fusionWaitingSecond;

    public List<GameObject> _wallDestroyed = new();
    private int _iteration;
    public bool _containItem;

    public NavMeshSurface _navMeshSurface;
    public List<NavMeshLink> _navChunkConnection = new();

    public override void CallGenerateMaze()
    {
        GenerateGrid(_cellPrefab.gameObject, _width, _height, _size);
        GenerateMazeFusion();
    }

    public void BakeNashMeshSurface()
    {
        if (_navMeshSurface == null)
        {
            if (gameObject.TryGetComponent(out NavMeshSurface surface))
            {
                _navMeshSurface = surface;
            }
            _navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
        }
        _navMeshSurface.BuildNavMesh();
    }

    private void GenerateGrid(GameObject cellPrefab, int width, int height, int cellSize)
    {
        if (cellPrefab == null || width <= 0 || height <= 0 || cellSize <= 0)
            return;

        _chunkCells.Clear();
        _iteration = 0;
        Vector3 worldOffset = transform.position;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize) + worldOffset;
                MazeCell newCell = Instantiate(cellPrefab, pos, Quaternion.identity, transform).GetComponent<MazeCell>();

                newCell.Init(_iteration);

                _chunkCells.Add(newCell);

                if (x > 0)
                {
                    MazeCell left = _chunkCells[y * width + (x - 1)];
                    newCell._neighbordsCells.Add(left);
                    left._neighbordsCells.Add(newCell);
                }
                if (y > 0)
                {
                    MazeCell down = _chunkCells[(y - 1) * width + x];
                    newCell._neighbordsCells.Add(down);
                    down._neighbordsCells.Add(newCell);
                }

                _iteration++;
            }
        }

        _isGenerated = true;
    }

    public void GenerateMazeFusion()
    {
        List<MazeCell> visited = new();
        Stack<MazeCell> stack = new();

        MazeCell start = _chunkCells[_rng.Next(0, _chunkCells.Count)];
        start._visited = true;
        visited.Add(start);
        stack.Push(start);

        while (stack.Count > 0)
        {
            // yield return new WaitForSeconds(_fusionWaitingSecond);
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

        double totalWeight = neighbors.Count * 0.5;
        double pick = _rng.NextDouble() * totalWeight;
        int index = (int)Math.Floor(pick / 0.5);
        return neighbors[Mathf.Clamp(index, 0, neighbors.Count - 1)];
    }

    private void DestroyWallWithOrientation(MazeCell current, MazeCell next)
    {
        int currentIndex = _chunkCells.IndexOf(current);
        int nextIndex = _chunkCells.IndexOf(next);

        int cx = currentIndex % _width;
        int cy = currentIndex / _width;
        int nx = nextIndex % _width;
        int ny = nextIndex / _width;

        if (nx == cx + 1) { current.DestroyWall(WallOrientation.Right, false, this); next.DestroyWall(WallOrientation.Left, false, this); }
        else if (nx == cx - 1) { current.DestroyWall(WallOrientation.Left, false, this); next.DestroyWall(WallOrientation.Right, false, this); }
        else if (ny == cy + 1) { current.DestroyWall(WallOrientation.Up, false, this); next.DestroyWall(WallOrientation.Down, false, this); }
        else if (ny == cy - 1) { current.DestroyWall(WallOrientation.Down, false, this); next.DestroyWall(WallOrientation.Up, false, this); }
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
            MazeCell cell = _chunkCells[_rng.Next(0, totalCells)];

            int index = _chunkCells.IndexOf(cell);
            int x = index % _width;
            int y = index / _width;

            if (x == 0 || y == 0 || x == _width - 1 || y == _height - 1)
                continue;

            WallOrientation dir = (WallOrientation)_rng.Next(0, 4);
            MazeCell neighbor = GetNeighbor(x, y, dir);

            if (neighbor == null)
                continue;

            cell.DestroyWall(dir, false, this);
            neighbor.DestroyWall(neighbor.GetOppositeWallOrientation(dir), false, this);

            destroyed++;
        }
    }

    private MazeCell GetNeighbor(int x, int y, WallOrientation dir)
    {
        return dir switch
        {
            WallOrientation.Right => (x + 1 < _width) ? _chunkCells[y * _width + (x + 1)] : null,
            WallOrientation.Left => (x - 1 >= 0) ? _chunkCells[y * _width + (x - 1)] : null,
            WallOrientation.Up => (y + 1 < _height) ? _chunkCells[(y + 1) * _width + x] : null,
            WallOrientation.Down => (y - 1 >= 0) ? _chunkCells[(y - 1) * _width + x] : null,
            _ => null
        };
    }
    public override void RegenerateMaze()
    {
        _rng = new System.Random(_seed);

        foreach (var tiles in _wallDestroyed)
        {
            tiles.gameObject.SetActive(true);
        }

        _wallDestroyed.Clear();

        for (int i = 0; i < _chunkCells.Count; i++)
        {
            _chunkCells[i]._visited = false;
            _chunkCells[i]._cellNumber = i;
        }

        GenerateMazeFusion();
    }

    public List<MazeCell> GetDeadEndCells()
    {
        List<MazeCell> deadEnds = new();

        foreach (var cell in _chunkCells)
        {
            int activeWallCount = 0;

            foreach (WallOrientation direction in System.Enum.GetValues(typeof(WallOrientation)))
            {
                int wallIndex = cell.GetWallIndex(direction);
                if (wallIndex < 0) continue;

                Transform wall = cell._wallContainer.GetChild(wallIndex);
                if (wall.gameObject.activeSelf)
                {
                    activeWallCount++;
                }
            }

            if (activeWallCount == 3)
            {
                deadEnds.Add(cell);
            }
        }

        return deadEnds;
    }
}
