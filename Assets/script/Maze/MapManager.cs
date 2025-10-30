using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;

public class MapManager : NetworkBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private MazeChunk _chunkLabyrinthPrefab;
    [SerializeField] private MazeChunk _chunkSafePrefab;
    [SerializeField, Range(0.0f, 1.0f)] private float _connectionChance = 0.15f;
    [SerializeField, Min(1)] private int _width = 5;
    [SerializeField, Min(1)] private int _height = 5;
    [SerializeField] private Vector2Int _chunkSize;
    private readonly List<MazeChunk> _mapChunks = new();
    public MazeChunkSafeZone _safeChunk;

    [Header("Spawn Item")]
    [SerializeField, Min(1)] private int _itemCountOnMap;
    [SerializeField, Min(1)] private Item _itemOnMap;

    [Header("Spawn Enemy")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int _maxEnemyToSpawn;

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

        if (_itemCountOnMap >= ((_width * _height) / 2) - 1) _itemCountOnMap = ((_width * _height) / 2) - 1;
    }

    public void StartMapGeneration()
    {
        if (!IsServer) return;

        GenerateChunkGrid();

        for (int i = 0; i < _itemCountOnMap; i++)
        {
            PlaceItem(_itemOnMap);
        }
        SpawnEnemyOnMap(_enemyPrefab, 1);
    }


    private void GenerateChunkGrid(List<int> seeds = null)
    {
        _mapChunks.Clear();

        Vector3 startOffset = new Vector3(
            -(_width / 2f) * _chunkSize.x,
            0,
            -(_height / 2f) * _chunkSize.y
        );

        List<int> chunkSeeds = new();

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                Vector3 pos = new Vector3(x * _chunkSize.x, 0, y * _chunkSize.y) + startOffset;
                bool isCenter = (x == _width / 2 && y == _height / 2);

                int seed = 0;

                if ((seeds == null || seeds.Count <= 0) && IsServer)
                {
                    seed = UnityEngine.Random.Range(0, int.MaxValue);
                    chunkSeeds.Add(seed);
                }
                else
                {
                    seed = seeds[y * _width + x];
                }

                MazeChunk prefab = isCenter ? _chunkSafePrefab : _chunkLabyrinthPrefab;
                MazeChunk newChunk = Instantiate(prefab, pos, Quaternion.identity, transform);
                newChunk.transform.name = $"Chunk ({x}, {y})";
                newChunk.Initialize(seed);
                _mapChunks.Add(newChunk);

                if (isCenter)
                {
                    _safeChunk = newChunk.GetComponent<MazeChunkSafeZone>();
                }
                else
                {
                    newChunk.GetComponent<MazeChunkLabyrinth>().BakeNashMeshSurface();
                }

                // Connexions horizontales
                if (x > 0)
                {
                    MazeChunk leftChunk = _mapChunks[y * _width + (x - 1)];
                    if (leftChunk is null) continue;

                    newChunk._neighbordsChunks.Add(leftChunk);
                    leftChunk._neighbordsChunks.Add(newChunk);
                    if (newChunk.TryGetComponent(out MazeChunkLabyrinth labA) && leftChunk.TryGetComponent(out MazeChunkLabyrinth labB))
                    {
                        StartCoroutine(ConnectAdjacentChunks(labA, labB, WallOrientation.Left));
                    }
                }

                // Connexions verticales
                if (y > 0)
                {
                    MazeChunk downChunk = _mapChunks[(y - 1) * _width + x];
                    if (downChunk is null) continue;

                    newChunk._neighbordsChunks.Add(downChunk);
                    downChunk._neighbordsChunks.Add(newChunk);
                    if (newChunk.TryGetComponent(out MazeChunkLabyrinth labA) && downChunk.TryGetComponent(out MazeChunkLabyrinth labB))
                    {
                        StartCoroutine(ConnectAdjacentChunks(labA, labB, WallOrientation.Down));
                    }
                }
            }
        }

        if (IsServer)
        {
            SendChunkSeedsToClientRpc(chunkSeeds.ToArray());
        }


        _safeChunk.TryOpenNeighbordWall();


        EventBus.Publish(EventType.MapGenerated, true);
    }



    [ClientRpc]
    private void SendChunkSeedsToClientRpc(int[] seeds)
    {
        if (IsServer || IsHost) return;

        StopAllCoroutines();
        GenerateChunkGrid(new List<int>(seeds));
    }

    private void SpawnEnemyOnMap(GameObject enemyPrefab, int nbsMaxTypeMob)
    {
        nbsMaxTypeMob = Mathf.Clamp(nbsMaxTypeMob, 0, 4);

        MazeChunk[] chunkSpawner = new MazeChunk[4];
        chunkSpawner[0] = _mapChunks[0];
        chunkSpawner[1] = _mapChunks[_width - 1];
        chunkSpawner[2] = _mapChunks[(_height - 1) * _width];
        chunkSpawner[3] = _mapChunks[(_width * _height) - 1];

        List<MazeChunk> availableChunks = new List<MazeChunk>(chunkSpawner);

        for (int i = 0; i < nbsMaxTypeMob; i++)
        {
            if (availableChunks.Count == 0)
                break;

            int randIndex = UnityEngine.Random.Range(0, availableChunks.Count);
            MazeChunkLabyrinth chosenChunk = availableChunks[randIndex].GetComponent<MazeChunkLabyrinth>();

            availableChunks.RemoveAt(randIndex);
            Vector3 spawnPos = chosenChunk._chunkCells[UnityEngine.Random.Range(0, chosenChunk._chunkCells.Count - 1)].transform.position;
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
    }


    private void PlaceItem(Item item)
    {
        if (!IsServer) return;


        List<MazeChunkLabyrinth> chunksWithDeadEnds = new();
        foreach (var chunk in _mapChunks)
        {
            if (chunk.TryGetComponent(out MazeChunkLabyrinth mazeLaby))
            {
                if (mazeLaby.GetDeadEndCells().Count > 0 && !mazeLaby._containItem)
                    chunksWithDeadEnds.Add(mazeLaby);
            }
        }

        if (chunksWithDeadEnds.Count == 0)
        {
            return;
        }

        MazeChunkLabyrinth selectedChunk = chunksWithDeadEnds[UnityEngine.Random.Range(0, chunksWithDeadEnds.Count)];
        List<MazeCell> deadEnds = selectedChunk.GetDeadEndCells();
        MazeCell selectedCell = deadEnds[UnityEngine.Random.Range(0, deadEnds.Count)];
        selectedChunk._containItem = true;

        GameObject newItemObject = Instantiate(item.gameObject, selectedCell.transform.position + Vector3.up * 0.5f, Quaternion.identity);

        if (newItemObject.TryGetComponent(out Item newItem))
        {
            if (newItem.TryGetComponent(out NetworkObject netObj))
            {
                netObj.Spawn(true);
            }
            else
            {
                newItemObject.AddComponent<NetworkObject>().Spawn(true);
            }
        }
    }




    #region Connect Chunk 

    private IEnumerator ConnectAdjacentChunks(MazeChunkLabyrinth labA, MazeChunkLabyrinth labB, WallOrientation direction)
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
                for (int y = 0; y < height; y++)
                {
                    if (labA._rng.NextDouble() <= _connectionChance)
                    {
                        ConnectChunkOnLeftDirection(labA, labB, y, width);
                        doorCreatedCount++;
                    }
                }
                if (doorCreatedCount == 0)
                {
                    int randomY = labA._rng.Next(0, height);
                    ConnectChunkOnLeftDirection(labA, labB, randomY, width);
                }
                break;

            case WallOrientation.Down:
                for (int x = 0; x < width; x++)
                {
                    if (labA._rng.NextDouble() <= _connectionChance)
                    {
                        ConnectChunkOnBottomDirection(labA, labB, x, width, height);
                        doorCreatedCount++;
                    }
                }
                if (doorCreatedCount == 0)
                {
                    int randomX = labA._rng.Next(0, width);
                    ConnectChunkOnBottomDirection(labA, labB, randomX, width, height);
                }
                break;
        }
    }
    private void ConnectChunkOnLeftDirection(MazeChunkLabyrinth labA, MazeChunkLabyrinth labB, int y, int width)
    {
        if (labA._chunkCells.Count <= 0 || labB._chunkCells.Count <= 0) return;

        MazeCell leftCellA = labA._chunkCells[y * width + 0];
        MazeCell rightCellB = labB._chunkCells[y * width + (width - 1)];

        leftCellA.DestroyWall(WallOrientation.Left, true, labA);
        rightCellB.DestroyWall(WallOrientation.Right, true, labB);

        labA.AddDoorPair(leftCellA, rightCellB, WallOrientation.Left);
        labB.AddDoorPair(rightCellB, leftCellA, WallOrientation.Right);

        ConnectNavMesh(leftCellA, rightCellB);
        ConnectNavMesh(rightCellB, leftCellA);


    }
    private void ConnectChunkOnBottomDirection(MazeChunkLabyrinth labA, MazeChunkLabyrinth labB, int x, int width, int height)
    {
        if (labA._chunkCells.Count <= 0 || labB._chunkCells.Count <= 0) return;

        MazeCell bottomCellA = labA._chunkCells[0 * width + x];
        MazeCell topCellB = labB._chunkCells[(height - 1) * width + x];

        bottomCellA.DestroyWall(WallOrientation.Down, true, labA);
        topCellB.DestroyWall(WallOrientation.Up, true, labB);

        labA.AddDoorPair(bottomCellA, topCellB, WallOrientation.Down);
        labB.AddDoorPair(topCellB, bottomCellA, WallOrientation.Up);


        ConnectNavMesh(bottomCellA, topCellB);
        ConnectNavMesh(topCellB, bottomCellA);
    }

    private void ConnectNavMeshChunkByDoor()
    {
        foreach (MazeChunk chunk in _mapChunks)
        {
            foreach (CellPair door in chunk._doorPairs)
            {
                ConnectNavMesh(door.localCell, door.neighborCell);
                ConnectNavMesh(door.neighborCell, door.localCell);
            }
        }
    }

    private void ConnectNavMesh(MazeCell a, MazeCell b)
    {
        NavMeshLink link = a.gameObject.AddComponent<NavMeshLink>();

        link.startTransform = a.transform;
        link.endTransform = b.transform;

        link.width = 4;
        link.autoUpdate = true;
    }
    #endregion
}

