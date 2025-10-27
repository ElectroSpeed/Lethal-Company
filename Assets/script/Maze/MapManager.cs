using System.Collections.Generic;
using System.Collections;
using Unity.Collections;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private MazeChunk _chunkLabyrinthPrefab;
    [SerializeField] private MazeChunk _chunkSafePrefab;

    [SerializeField, Min(1)] private int _width = 5;
    [SerializeField, Min(1)] private int _height = 5;
    [SerializeField] private Vector2Int _chunkSize = new Vector2Int(50, 50);

    private readonly List<MazeChunk> _mapChunks = new();

    private void OnValidate()
    {
        if (_width % 2 == 0) _width++;
        if (_height % 2 == 0) _height++;
        
        if (_chunkLabyrinthPrefab != null)
        {
            _chunkSize = new Vector2Int(
                _chunkLabyrinthPrefab._width * _chunkLabyrinthPrefab._size,
                _chunkLabyrinthPrefab._height * _chunkLabyrinthPrefab._size
            );
        }
    }

    private void Start()
    {
        GenerateChunkGrid();
    }

    private void GenerateChunkGrid()
    {
        if (!_chunkLabyrinthPrefab || !_chunkSafePrefab)
        {
            return;
        }

        _mapChunks.Clear();

        Vector3 startOffset = new Vector3(
            -(_width / 2f) * _chunkSize.x,
            0,
            -(_height / 2f) * _chunkSize.y
        );
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                Vector3 pos = new Vector3(x * _chunkSize.x, 0, y * _chunkSize.y) + startOffset;
                bool isCenter = (x == _width / 2 && y == _height / 2);

                MazeChunk prefab = isCenter ? _chunkSafePrefab : _chunkLabyrinthPrefab;
                MazeChunk newChunk = Instantiate(prefab, pos, Quaternion.identity, transform);
                _mapChunks.Add(newChunk);

                MazeChunkLabyrinth currentChunk = newChunk.GetComponent<MazeChunkLabyrinth>();

                if (currentChunk == null)
                    continue;
                
                if (x > 0)
                {
                    MazeChunkLabyrinth leftChunk = _mapChunks[y * _width + (x - 1)].GetComponent<MazeChunkLabyrinth>();
                    currentChunk._neighbordsChunks.Add(leftChunk);
                    leftChunk._neighbordsChunks.Add(currentChunk);
                    StartCoroutine(ConnectAdjacentChunks(currentChunk, leftChunk, WallOrientation.Left));
                }
                
                if (y > 0)
                {
                    MazeChunkLabyrinth downChunk = _mapChunks[(y - 1) * _width + x].GetComponent<MazeChunkLabyrinth>();
                    currentChunk._neighbordsChunks.Add(downChunk);
                    downChunk._neighbordsChunks.Add(currentChunk);
                    StartCoroutine(ConnectAdjacentChunks(currentChunk, downChunk, WallOrientation.Down));
                }
            }
        }
    }

    private IEnumerator ConnectAdjacentChunks(MazeChunkLabyrinth labA, MazeChunkLabyrinth labB, WallOrientation direction, float connectionChance = 0.85f)
    {
        yield return new WaitUntil(() => labA._isGenerated || labB._isGenerated);
        
        int width = labA._width;
        int height = labA._height;

        switch (direction)
        {
            case WallOrientation.Left:
                for (int y = 0; y < height; y++)
                {
                    if (connectionChance <= Random.Range(0f, 1f))
                    {
                        MazeCell leftCellA = labA._chunkCells[y * width + 0];
                        MazeCell rightCellB = labB._chunkCells[y * width + (width - 1)];

                        leftCellA.DestroyWall(WallOrientation.Left, true, labA);
                        rightCellB.DestroyWall(WallOrientation.Right, true, labB);
                    }
                }
                break;

            case WallOrientation.Down:
                for (int x = 0; x < width; x++)
                {
                    if (connectionChance <= Random.Range(0f, 1f))
                    {
                        MazeCell bottomCellA = labA._chunkCells[0 * width + x];
                        MazeCell topCellB = labB._chunkCells[(height - 1) * width + x];

                        bottomCellA.DestroyWall(WallOrientation.Down, true, labA);
                        topCellB.DestroyWall(WallOrientation.Up, true, labB);
                    }
                }
                break;
        }
    }

    [ContextMenu("TestGenerateWalls")]
    public void TestGenerateWalls()
    {
        foreach (MazeChunkLabyrinth laby in _mapChunks)
        {
            foreach (var tiles in laby._wallDestroyed)
            {
                tiles.gameObject.SetActive(true);
            }
            
            laby._wallDestroyed.Clear();

            for (int i = 0; i < laby._chunkCells.Count; i++)
            {
                laby._chunkCells[i]._visited = false;
                laby._chunkCells[i]._cellNumber = i;
            }
            
            laby.GenerateMazeFusion();
        }
    }
}
