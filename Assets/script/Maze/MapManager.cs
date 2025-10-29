using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

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

    public NetworkList<int> _chunkSeeds = new();

    [SerializeField, Min(1)] private int _itemCountOnMap;
    [SerializeField, Min(1)] private Item _itemOnMap;

    private bool _canStartMapGen;
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
    private void OnEnable()
    {
        EventBus.Subscribe<bool>(EventType.AllPlayerWasConnected, CanStartMapGeneration);
        Debug.Log("[MapManager] Subscribed to AllPlayerWasConnected event");
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<bool>(EventType.AllPlayerWasConnected, CanStartMapGeneration);
        Debug.Log("[MapManager] Unsubscribed from AllPlayerWasConnected event");
    }

    public void CanStartMapGeneration(bool value)
    {
        _canStartMapGen = true;
        Debug.Log("[MapManager] Received AllPlayerWasConnected event -> can start map generation");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"``````````````````[MapManager] OnNetworkSpawn ({(IsServer ? "Server" : "Client")})``````````````````");

        //StartCoroutine(WaitForPlayerConnected());
        _chunkSeeds.OnListChanged += OnChunkSeedsChanged;
    }
    public override void OnNetworkDespawn()
    {
        _chunkSeeds.OnListChanged -= OnChunkSeedsChanged;
        Debug.Log("[MapManager] OnNetworkDespawn: unsubscribed from _chunkSeeds event");
    }

    public IEnumerator WaitForPlayerConnected()
    {
        Debug.Log("[MapManager] Waiting for players to connect before generating map...");
        yield return new WaitUntil(() => _canStartMapGen == true);
       //yield return new WaitForEndOfFrame();
        Debug.Log("---------------------[MapManager - MAP] Try to all client to Generate map need filtrer..------------------- ");
        if (IsServer)
        {
            Debug.Log("[MapManager] All players connected - starting server-side map generation");
            StartCoroutine(GenerateChunkGrid());

            for (int i = 0; i < _itemCountOnMap; i++)
            {
                PlaceItem(_itemOnMap);
            }
        }
        else
        {
            Debug.Log("[MapManager] Client waiting for server to send chunk seeds...");
            OnChunkSeedsChanged(default);
            StartCoroutine(WaitMapIsGenerated());
        }
    }

    private void OnChunkSeedsChanged(NetworkListEvent<int> changeEvent)
    {
        Debug.Log($"-------------[SERVER] Server Try to Regenerate Map )-------------------------------");
        if (IsServer || IsHost) return;

        Debug.Log($"-------------[CLIENT] OnChunkSeedsChanged triggered ({_chunkSeeds.Count}/{_width * _height})-------------------------------");

        if (_chunkSeeds.Count == _width * _height)
        {
            Debug.Log("[CLIENT] All seeds received, starting GenerateChunkGrid");
            List<int> seedsCopy = new();
            foreach (var seed in _chunkSeeds)
                seedsCopy.Add(seed);

            StartCoroutine(GenerateChunkGrid(seedsCopy));
        }
    }

    private IEnumerator WaitMapIsGenerated()
    {
        Debug.Log("[CLIENT] WaitMapIsGenerated started, waiting for full seed list...");
        yield return new WaitUntil(() => _chunkSeeds != null && _chunkSeeds.Count == _width * _height);
        Debug.Log("[CLIENT] All chunk seeds received. Ready to build map.");
    }

    private IEnumerator GenerateChunkGrid(List<int> seeds = null)
    {
        Debug.Log($"[MAP] GenerateChunkGrid started on {(IsServer ? "SERVER" : "CLIENT")}");

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
                    seed = Random.Range(0, int.MaxValue);
                    chunkSeeds.Add(seed);
                    Debug.Log($"[SERVER] Generated seed {seed} for chunk ({x},{y})");
                }
                else
                {
                    seed = seeds[y * _height + x];
                }

                MazeChunk prefab = isCenter ? _chunkSafePrefab : _chunkLabyrinthPrefab;
                MazeChunk newChunk = Instantiate(prefab, pos, Quaternion.identity, transform);
                newChunk.Initialize(seed);
                _mapChunks.Add(newChunk);
                Debug.Log($"[MAP] Chunk ({x},{y}) created -> {(isCenter ? "SAFE ZONE" : "LABYRINTH")} with seed {seed}");

                if (isCenter)
                {
                    _safeChunk = newChunk.GetComponent<MazeChunkSafeZone>();
                    Debug.Log("[MAP] SafeChunk assigned.");
                }

                // Connexions horizontales
                if (x > 0)
                {
                    MazeChunk leftChunk = _mapChunks[y * _width + (x - 1)];
                    if (leftChunk is null) continue;

                    newChunk._neighbordsChunks.Add(leftChunk);
                    leftChunk._neighbordsChunks.Add(newChunk);

                    StartCoroutine(ConnectAdjacentChunks(newChunk, leftChunk, WallOrientation.Left));
                }

                // Connexions verticales
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

        if (IsServer)
        {
            foreach (var seed in chunkSeeds)
                _chunkSeeds.Add(seed);

            Debug.Log($"[SERVER] Added {chunkSeeds.Count} seeds to _chunkSeeds NetworkList");
            Debug.Log($"[MAP] GenerateChunkGrid completed on {(IsServer ? "SERVER" : "CLIENT")}");
        }

        yield return StartCoroutine(MapGenerated());
    }

    private void OpenMiddleDoor()
    {
        Debug.Log("[MAP] Opening middle door (safe zone neighbor walls)");
        _safeChunk.TryOpenNeighbordWall();
    }

    private IEnumerator MapGenerated()
    {
        Debug.Log($"[MAP] Waiting for {_mapChunks.Count} chunks to be fully generated...");
        OpenMiddleDoor();

        yield return new WaitForEndOfFrame();

        Debug.Log("[MAP] All chunks are ready -> publishing MapGenerated event");
        EventBus.Publish(EventType.MapGenerated, true);
    }

    private void PlaceItem(Item item)
    {
        if (!IsHost) return;

        Debug.Log("[SERVER] Try to spawn collectible item");

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
            Debug.LogWarning("[SERVER] No valid dead-end chunk found for item placement!");
            return;
        }

        MazeChunkLabyrinth selectedChunk = chunksWithDeadEnds[Random.Range(0, chunksWithDeadEnds.Count)];
        List<MazeCell> deadEnds = selectedChunk.GetDeadEndCells();
        MazeCell selectedCell = deadEnds[Random.Range(0, deadEnds.Count)];
        selectedChunk._containItem = true;

        GameObject newItemObject = Instantiate(item.gameObject, selectedCell.transform.position + Vector3.up * 0.5f, Quaternion.identity);
        Debug.Log($"[SERVER] Spawned local item {newItemObject.name} at {selectedCell.transform.position}");

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

            Debug.Log($"[SERVER] Item {newItemObject.name} spawned and replicated for all clients");
        }
    }

    private IEnumerator ConnectAdjacentChunks(MazeChunk labA, MazeChunk labB, WallOrientation direction)
    {
        if (labA is not MazeChunkLabyrinth && labB is not MazeChunkLabyrinth)
            yield break;

        yield return new WaitUntil(() => labA._isGenerated && labB._isGenerated);

        Debug.Log($"[MAP] Connecting chunks {labA.name} <-> {labB.name} ({direction})");

        int width = labA._width;
        int height = labA._height;
        int doorCreatedCount = 0;

        switch (direction)
        {
            case WallOrientation.Left:
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

            case WallOrientation.Down:
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

        Debug.Log($"[MAP] Connected {labA.name} <-> {labB.name} with {doorCreatedCount} door(s) ({direction})");
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
}
