using System.Collections;
using System.Collections.Generic;
<<<<<<< Updated upstream
using TMPro;
=======
using Unity.Netcode;
>>>>>>> Stashed changes
using UnityEngine;

public class MapManager : NetworkBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private MazeChunk _chunkLabyrinthPrefab;
    [SerializeField] private MazeChunk _chunkSafePrefab;
    [SerializeField, Range(0.0f, 1.0f)] private float _connectionChance = 0.15f;

    [SerializeField, Min(1)] private int _width = 5;
    [SerializeField, Min(1)] private int _height = 5;
    [SerializeField] private Vector2Int _chunkSize = new Vector2Int(50, 50);

    private readonly List<MazeChunk> _mapChunks = new();
<<<<<<< Updated upstream

    public MazeChunkSafeZone _safeChunk;

=======
    public MazeChunk _safeChunk;
>>>>>>> Stashed changes

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

<<<<<<< Updated upstream
























    private void Awake()
=======
    public override void OnNetworkSpawn()
>>>>>>> Stashed changes
    {
        if (IsServer)
        {
            GenerateChunkGrid();
        }
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

<<<<<<< Updated upstream
                _mapChunks.Add(newChunk);

                if (isCenter) _safeChunk = newChunk.GetComponent<MazeChunkSafeZone>();


                if (x > 0)
                {
                    MazeChunk leftChunk = _mapChunks[y * _width + (x - 1)];
                    if (leftChunk is null) continue;

=======
                NetworkObject netObj = newChunk.GetComponent<NetworkObject>();
                if (netObj != null)
                    netObj.Spawn(true);

                if (isCenter)
                    _safeChunk = newChunk;

                _mapChunks.Add(newChunk);

                // Connexions entre chunks
                if (x > 0)
                {
                    MazeChunk leftChunk = _mapChunks[y * _width + (x - 1)];
>>>>>>> Stashed changes
                    newChunk._neighbordsChunks.Add(leftChunk);
                    leftChunk._neighbordsChunks.Add(newChunk);
                    StartCoroutine(ConnectAdjacentChunks(newChunk, leftChunk, WallOrientation.Left));
                }

                if (y > 0)
                {
                    MazeChunk downChunk = _mapChunks[(y - 1) * _width + x];
<<<<<<< Updated upstream
                    if (downChunk is null) continue;


=======
>>>>>>> Stashed changes
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
        yield return new WaitUntil(() => labA._isGenerated && labB._isGenerated);
        ConnectChunksClientRpc(labA.NetworkObjectId, labB.NetworkObjectId, direction);
    }

    [ClientRpc]
    private void ConnectChunksClientRpc(ulong aId, ulong bId, WallOrientation direction)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(aId, out var aObj)) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bId, out var bObj)) return;

        MazeChunk labA = aObj.GetComponent<MazeChunk>();
        MazeChunk labB = bObj.GetComponent<MazeChunk>();

        if (labA == null || labB == null) return;

        switch (direction)
        {
            case WallOrientation.Left:
                ConnectChunkOnLeftDirection(labA, labB, labA._height, labA._width);
                break;
            case WallOrientation.Down:
                ConnectChunkOnBottomDirection(labA, labB, labA._width, labA._height);
                break;
        }
    }

    private void ConnectChunkOnLeftDirection(MazeChunk labA, MazeChunk labB, int height, int width)
    {
<<<<<<< Updated upstream
        if (labA._chunkCells.Count <= 0 || labB._chunkCells.Count <= 0) return;

        MazeCell leftCellA = labA._chunkCells[y * width + 0];
        MazeCell rightCellB = labB._chunkCells[y * width + (width - 1)];

=======
        int randomY = Random.Range(0, height);
        MazeCell leftCellA = labA._chunkCells[randomY * width + 0];
        MazeCell rightCellB = labB._chunkCells[randomY * width + (width - 1)];
>>>>>>> Stashed changes
        leftCellA.DestroyWall(WallOrientation.Left, true, (MazeChunkLabyrinth)labA);
        rightCellB.DestroyWall(WallOrientation.Right, true, (MazeChunkLabyrinth)labB);
    }

    private void ConnectChunkOnBottomDirection(MazeChunk labA, MazeChunk labB, int width, int height)
    {
<<<<<<< Updated upstream
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
=======
        int randomX = Random.Range(0, width);
        MazeCell bottomCellA = labA._chunkCells[0 * width + randomX];
        MazeCell topCellB = labB._chunkCells[(height - 1) * width + randomX];
        bottomCellA.DestroyWall(WallOrientation.Down, true, (MazeChunkLabyrinth)labA);
        topCellB.DestroyWall(WallOrientation.Up, true, (MazeChunkLabyrinth)labB);
>>>>>>> Stashed changes
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
