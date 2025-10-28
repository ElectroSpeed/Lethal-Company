using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private MazeChunk _chunkLabyrinthPrefab;
    [SerializeField] private MazeChunk _chunkSafePrefab;
    [SerializeField, Range(0.0f, 1.0f)] private float _connectionChance = 0.15f;


    [SerializeField, Min(1)] private int _width = 5;
    [SerializeField, Min(1)] private int _height = 5;
    [SerializeField] private Vector2Int _chunkSize = new Vector2Int(50, 50);

    private readonly List<MazeChunk> _mapChunks = new();

    public MazeChunkSafeZone _safeChunk;

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

























    private void Awake()
    {
        GenerateChunkGrid();
    }

    private void GenerateChunkGrid()
    {
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

                if (isCenter) _safeChunk = newChunk.GetComponent<MazeChunkSafeZone>();


                if (x > 0)
                {
                    MazeChunk leftChunk = _mapChunks[y * _width + (x - 1)];
                    if (leftChunk is null) continue;

                    newChunk._neighbordsChunks.Add(leftChunk);
                    leftChunk._neighbordsChunks.Add(newChunk);
                    StartCoroutine(ConnectAdjacentChunks(newChunk, leftChunk, WallOrientation.Left));
                }

                if (y > 0)
                {
                    MazeChunk downChunk = _mapChunks[(y - 1) * _width + x];
                    if (downChunk is null) continue;


                    newChunk._neighbordsChunks.Add(downChunk);
                    downChunk._neighbordsChunks.Add(newChunk);
                    StartCoroutine(ConnectAdjacentChunks(newChunk, downChunk, WallOrientation.Down));
                }
            }
        }
        StartCoroutine(MapGenerated());
    }

    private IEnumerator MapGenerated()
    {
        foreach (var a in _mapChunks)
        {
            yield return new WaitUntil(() => a._isGenerated);
        }

        OpenMiddleDoor();


        EventBus.Publish(EventType.MapGenerated, true);
    }



    private void OpenMiddleDoor()
    {
        _safeChunk.TryOpenNeighbordWall();
    }

    private IEnumerator ConnectAdjacentChunks(MazeChunk labA, MazeChunk labB, WallOrientation direction)
    {
        if (labA is not MazeChunkLabyrinth && labB is not MazeChunkLabyrinth)
            yield break;

        yield return new WaitUntil(() => labA._isGenerated && labB._isGenerated);

        int width = labA._width;
        int height = labA._height;
        int doorCreatedCount = 0;

        switch (direction)
        {
            case WallOrientation.Left:
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (Random.value <= _connectionChance)
                        {
                            ConnectChunkOnLeftDirection(labA, labB, y, width);
                            doorCreatedCount++;
                        }
                    }
                    if (doorCreatedCount == 0)
                    {
                        int randomY = Random.Range(0, height);
                        ConnectChunkOnLeftDirection(labA, labB, randomY, width);
                    }
                    break;
                }

            case WallOrientation.Down:
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (Random.value <= _connectionChance)
                        {
                            ConnectChunkOnBottomDirection(labA, labB, x, width, height);
                            doorCreatedCount++;
                        }
                    }
                    if (doorCreatedCount == 0)
                    {
                        int randomX = Random.Range(0, width);
                        ConnectChunkOnBottomDirection(labA, labB, randomX, width, height);
                    }
                    break;
                }
        }
    }


    private void ConnectChunkOnLeftDirection(MazeChunk labA, MazeChunk labB, int y, int width)
    {
        if (labA._chunkCells.Count <= 0 || labB._chunkCells.Count <= 0) return;

        MazeCell leftCellA = labA._chunkCells[y * width + 0];
        MazeCell rightCellB = labB._chunkCells[y * width + (width - 1)];

        leftCellA.DestroyWall(WallOrientation.Left, true, (MazeChunkLabyrinth)labA);
        rightCellB.DestroyWall(WallOrientation.Right, true, (MazeChunkLabyrinth)labB);

        labA.AddDoorPair(leftCellA, rightCellB, WallOrientation.Left);
        labB.AddDoorPair(rightCellB, leftCellA, WallOrientation.Right);
    }

    private void ConnectChunkOnBottomDirection(MazeChunk labA, MazeChunk labB, int x, int width, int height)
    {
        if (labA._chunkCells.Count <= 0 || labB._chunkCells.Count <= 0) return;

        MazeCell bottomCellA = labA._chunkCells[0 * width + x];
        MazeCell topCellB = labB._chunkCells[(height - 1) * width + x];

        bottomCellA.DestroyWall(WallOrientation.Down, true, (MazeChunkLabyrinth)labA);
        topCellB.DestroyWall(WallOrientation.Up, true, (MazeChunkLabyrinth)labB);


        labA.AddDoorPair(bottomCellA, topCellB, WallOrientation.Down);
        labB.AddDoorPair(topCellB, bottomCellA, WallOrientation.Up);

    }




    #region Utility function 
    public void RegenerateChunkMaze(MazeChunkLabyrinth labyToRegenerate)
    {
        labyToRegenerate.RegenerateMaze();
    }
    [ContextMenu("Map/Regenerate Random Maze")]
    public void TEST_RegenerateFirstChunkLabyrinth()
    {
        MazeChunk labyToRegenerate = _mapChunks[0];
        labyToRegenerate.RegenerateMaze();
    }

    [ContextMenu("Map/Close First Chunk Door")]
    public void TEST_CloseFirstChunkDoor()
    {
        MazeChunk laby = _mapChunks[0];
        CloseWallsForChunk(laby);
    }

    public void CloseWallsForChunk(MazeChunk chunk)
    {
        foreach (CellPair wallPair in chunk._doorPairs)
        {
            wallPair.localCell.CloseWall(wallPair.orientation);

            WallOrientation opposite = wallPair.neighborCell.GetOppositeWallOrientation(wallPair.orientation);
            wallPair.neighborCell.CloseWall(opposite);
        }
    }

    [ContextMenu("Map/Open First Chunk Door")]
    public void TEST_OpenFirstChunkDoor()
    {
        MazeChunk laby = _mapChunks[0];
        ReopenChunkWall(laby);
    }
    public void ReopenChunkWall(MazeChunk chunk)
    {
        foreach (CellPair wallPair in chunk._doorPairs)
        {
            wallPair.localCell.DestroyWall(wallPair.orientation, true);

            WallOrientation opposite = wallPair.neighborCell.GetOppositeWallOrientation(wallPair.orientation);
            wallPair.neighborCell.DestroyWall(opposite, true);
        }
    }

    [ContextMenu("TEST_OpenCenterWall")]
    public void TEST_OpenCenterWall()
    {
        foreach (var chunk in _mapChunks)
        {
            if (chunk.GetComponent<MazeChunkSafeZone>() != null)
            {
                chunk.GetComponent<MazeChunkSafeZone>().TryOpenNeighbordWall();
            }
        }
    }
    #endregion
}
