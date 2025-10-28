using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeChunkLabyrinth : MazeChunk
{
    [Header("Maze Generation Settings")]
    [SerializeField, Range(0f, 1f)] private float _percentWallDestroyed = 0.15f;
    [SerializeField] private float _fusionWaitingSecond;

    public List<GameObject> _wallDestroyed = new();
    private int _iteration;

    public override void CallGenerateMaze()
    {
        if (!IsServer) return;
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

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize) + worldOffset;
                MazeCell newCell = Instantiate(cellPrefab, pos, Quaternion.identity, transform).GetComponent<MazeCell>();
                newCell.Init(_iteration);

                NetworkObject netObj = newCell.GetComponent<NetworkObject>();
                if (netObj != null)
                    netObj.Spawn(true);

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

    private void GenerateMazeFusion()
    {
        List<MazeCell> visited = new();
        Stack<MazeCell> stack = new();

        MazeCell start = _chunkCells[Random.Range(0, _chunkCells.Count)];
        start._visited = true;
        visited.Add(start);
        stack.Push(start);

        while (stack.Count > 0)
        {
            MazeCell current = stack.Pop();
            List<MazeCell> neighbors = new();

            foreach (var n in current._neighbordsCells)
                if (!n._visited)
                    neighbors.Add(n);

            if (neighbors.Count > 0)
            {
                stack.Push(current);
                MazeCell next = neighbors[Random.Range(0, neighbors.Count)];

                next._cellNumber = current._cellNumber;
                DestroyWallWithOrientation(current, next);

                next._visited = true;
                visited.Add(next);
                stack.Push(next);
            }
        }

        OnGenerationCompletedClientRpc();
    }

    [ClientRpc]
    private void OnGenerationCompletedClientRpc()
    {
        _isGenerated = true;
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

    public override void RegenerateMaze()
    {
        throw new System.NotImplementedException();
    }
}
