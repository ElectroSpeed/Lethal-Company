using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NavMeshSurface))]
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
    [Min(1)] public int _objectiveFlowerItemMaxCount;
    [SerializeField] private Item _objectifItemFlower;

    [Min(1)] public int _callMonsterItemMaxCount;
    [SerializeField] private Trap _callMonsterItem;


    [Header("Spawn Enemy")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int _maxEnemyToSpawn;

    [Header("map nav mesh")]
    [SerializeField] private NavMeshSurface _nav;

    private bool _mapInitialized = false;
    public bool IsMapReady => _mapInitialized;

    private void OnValidate()
    {
        if (_width % 2 == 0) _width++;
        if (_height % 2 == 0) _height++;

        if (_chunkLabyrinthPrefab != null)
        {
            _chunkSize = new Vector2Int(
                _chunkLabyrinthPrefab._width * _chunkLabyrinthPrefab._cellSize,
                _chunkLabyrinthPrefab._height * _chunkLabyrinthPrefab._cellSize
            );
        }

        if (_objectiveFlowerItemMaxCount >= ((_width * _height) / 2) - 1)
            _objectiveFlowerItemMaxCount = ((_width * _height) / 2) - 1;
    }

    private void Awake()
    {
        if (_nav is null)
        {
            if (TryGetComponent(out NavMeshSurface navMeshSurface))
            {
                _nav = navMeshSurface;
            }
            _nav = gameObject.AddComponent<NavMeshSurface>();
        }
    }

    public void OnEnable()
    {
        EventBus.Subscribe<EnemyBT>(EventType.FillEnemyPath, FillEnemyPath);
    }

    public void OnDisable()
    {
        EventBus.Unsubscribe<EnemyBT>(EventType.FillEnemyPath, FillEnemyPath);
    }

    public void FillEnemyPath(EnemyBT enemyBT)
    {
        enemyBT.SetEnemyPathOnMap(GetRandomCellsOnMap());
    }

    public void StartMapGeneration()
    {
        if (!IsServer) return;
        StartCoroutine(GenerateChunkGrid());
    }

    private IEnumerator GenerateChunkGrid(List<int> seeds = null)
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

                if (x > 0)
                {
                    MazeChunk leftChunk = _mapChunks[y * _width + (x - 1)];
                    if (leftChunk is null) continue;

                    newChunk._neighbordsChunks.Add(leftChunk);
                    leftChunk._neighbordsChunks.Add(newChunk);
                    if (newChunk.TryGetComponent(out MazeChunkLabyrinth labA) && leftChunk.TryGetComponent(out MazeChunkLabyrinth labB))
                    {
                        yield return StartCoroutine(ConnectAdjacentChunks(labA, labB, WallOrientation.Left));
                    }
                }

                if (y > 0)
                {
                    MazeChunk downChunk = _mapChunks[(y - 1) * _width + x];
                    if (downChunk is null) continue;

                    newChunk._neighbordsChunks.Add(downChunk);
                    downChunk._neighbordsChunks.Add(newChunk);
                    if (newChunk.TryGetComponent(out MazeChunkLabyrinth labA) && downChunk.TryGetComponent(out MazeChunkLabyrinth labB))
                    {
                        yield return StartCoroutine(ConnectAdjacentChunks(labA, labB, WallOrientation.Down));
                    }
                }
            }
        }

        if (IsServer)
        {
            SendChunkSeedsToClientRpc(chunkSeeds.ToArray());
        }

        _safeChunk.TryOpenNeighbordWall();

        for (int i = 0; i < _objectiveFlowerItemMaxCount; i++)
        {
            PlaceItem(_objectifItemFlower);
        }
        for (int i = 0; i < _callMonsterItemMaxCount; i++)
        {
            PlaceTrap(_callMonsterItem);
        }


        SpawnEnemyOnMap(_enemyPrefab, 2);
        _nav.BuildNavMesh();
        _mapInitialized = true;

        EventBus.Publish(EventType.MapGenerated, true);
    }

    [ClientRpc]
    private void SendChunkSeedsToClientRpc(int[] seeds)
    {
        if (IsServer || IsHost) return;
        StopAllCoroutines();
        StartCoroutine(GenerateChunkGrid(new List<int>(seeds)));
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
            EnemyBT enemyBT = newEnemy.GetComponent<EnemyBT>();
            enemyBT.Initialize(this);
            enemyBT.SetEnemyPathOnMap(GetRandomCellsOnMap());
            EventBus.Publish<EnemyBT>(EventType.SpawnEnemy, enemyBT);

            if (newEnemy.TryGetComponent(out NetworkObject NetObj))
            {
                NetObj.Spawn(true);
            }
            else
            {
                newEnemy.AddComponent<NetworkObject>().Spawn(true);
            }
        }
    }

    public List<MazeCell> GetRandomCellsOnMap()
    {
        int maxCell = 30;
        List<MazeCell> cells = new List<MazeCell>();
        List<MazeChunk> chunks = new List<MazeChunk>();

        foreach (MazeChunk chunk in _mapChunks)
        {
            if (chunk is MazeChunkSafeZone) continue;
            chunks.Add(chunk);
        }

        for (int i = 0; i < maxCell; i++)
        {
            MazeChunk randomChunkOnMap = chunks[UnityEngine.Random.Range(0, chunks.Count)];
            MazeCell randomCellOnChunk = randomChunkOnMap._chunkCells[UnityEngine.Random.Range(0, randomChunkOnMap._chunkCells.Count)];
            cells.Add(randomCellOnChunk);
        }

        return cells;
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

        if (chunksWithDeadEnds.Count == 0) return;

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

    private void PlaceTrap(Trap trap)
    {
        if (!IsServer) return;

        //////////////////////////////A Changer pour avoir des traps un peu partout sur la map/////////////////////////////////
        List<MazeChunkLabyrinth> chunksWithDeadEnds = new();
        foreach (var chunk in _mapChunks)
        {
            if (chunk.TryGetComponent(out MazeChunkLabyrinth mazeLaby))
            {
                if (mazeLaby.GetDeadEndCells().Count > 0 && !mazeLaby._containItem)
                    chunksWithDeadEnds.Add(mazeLaby);
            }
        }

        if (chunksWithDeadEnds.Count == 0) return;
        MazeChunkLabyrinth selectedChunk = chunksWithDeadEnds[UnityEngine.Random.Range(0, chunksWithDeadEnds.Count)];
        List<MazeCell> deadEnds = selectedChunk.GetDeadEndCells();
        MazeCell selectedCell = deadEnds[UnityEngine.Random.Range(0, deadEnds.Count)];

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        selectedChunk._containItem = true;

        GameObject newItemObject = Instantiate(trap.gameObject, selectedCell.transform.position, Quaternion.identity);

        if (newItemObject.TryGetComponent(out Trap newtrap))
        {
            if (newtrap.TryGetComponent(out NetworkObject netObj))
            {
                netObj.Spawn(true);
            }
            else
            {
                newItemObject.AddComponent<NetworkObject>().Spawn(true);
            }
        }
    }

    private IEnumerator ConnectAdjacentChunks(MazeChunkLabyrinth labA, MazeChunkLabyrinth labB, WallOrientation direction)
    {
        if (labA is not MazeChunkLabyrinth && labB is not MazeChunkLabyrinth) yield break;

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
    }
    
    public MazeChunk GetChunkFromWorldPosition(Vector3 worldPos)
    {
        foreach (MazeChunk chunk in _mapChunks)
        {
            if (chunk != null && chunk.Contains(worldPos))
                return chunk;
        }

        return null;
    }


    public List<Vector3> GetRandomCellsInChunk(MazeChunk chunk, int count)
    {
        List<Vector3> result = new();
        List<MazeCell> available = new List<MazeCell>(chunk._chunkCells); 

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, available.Count);
            result.Add(available[randomIndex].transform.position);
            available.RemoveAt(randomIndex);
        }

        return result;
    }

    #region Regeneration Map

        public void RegenerateChunksAroundPlayer(MazeChunk centerChunk, int range)
        {
            if (!IsServer || centerChunk == null) return;

            List<MazeChunkLabyrinth> chunksToRegen = new();

            foreach (MazeChunk chunk in _mapChunks)
            {
                if (chunk == null || chunk is not MazeChunkLabyrinth laby) continue;

                int dx = Mathf.Abs(GetChunkX(chunk) - GetChunkX(centerChunk));
                int dy = Mathf.Abs(GetChunkY(chunk) - GetChunkY(centerChunk));

                if (((dx <= range && dy == 0) || (dy <= range && dx == 0)) && chunk != centerChunk)
                {
                    if (!ChunkContainsImportantEntity(chunk))
                    {
                        laby._seed = UnityEngine.Random.Range(0, int.MaxValue);
                        chunksToRegen.Add(laby);
                    }
                }
            }

            foreach (var laby in chunksToRegen)
            {
                StartCoroutine(RegenerateChunk(laby, laby._seed));
            }

            SendChunkSeedsToClientsClientRpc(
                chunksToRegen.ConvertAll(c => _mapChunks.IndexOf(c)).ToArray(),
                chunksToRegen.ConvertAll(c => c._seed).ToArray()
            );

            _nav.BuildNavMesh();
        }

        [ClientRpc]
        private void SendChunkSeedsToClientsClientRpc(int[] chunkIndices, int[] seeds)
        {
            if (IsServer) return;

            for (int i = 0; i < chunkIndices.Length; i++)
            {
                MazeChunk chunk = _mapChunks[chunkIndices[i]];
                if (chunk == null) continue;

                StartCoroutine(WaitAndRegenerateChunk(chunk, seeds[i]));
            }
        }

        private IEnumerator WaitAndRegenerateChunk(MazeChunk chunk, int seed)
        {
            yield return new WaitUntil(() => chunk._isGenerated);
            StartCoroutine(RegenerateChunk(chunk, seed));
        }

        private IEnumerator RegenerateChunk(MazeChunk chunk, int seed)
        {
            if (chunk == null) yield break;

            if (chunk is MazeChunkLabyrinth laby)
            {
                laby._seed = seed;
                laby.RegenerateMaze();
                yield break;
            }

            yield return null;
        }

        private bool ChunkContainsImportantEntity(MazeChunk chunk)
        {
            if (chunk is MazeChunkLabyrinth laby && laby._containItem) return true;

            Collider[] colliders = Physics.OverlapBox(
                chunk.transform.position + new Vector3(_chunkSize.x / 2f, 0f, _chunkSize.y / 2f),
                new Vector3(_chunkSize.x / 2f, 10f, _chunkSize.y / 2f)
            );

            foreach (var col in colliders)
            {
                if (col.CompareTag("Enemy") || col.CompareTag("Player"))
                    return true;
            }

            return false;
        }

        private int GetChunkX(MazeChunk chunk)
        {
            int index = _mapChunks.IndexOf(chunk);
            if (index < 0) return -1;
            return index % _width;
        }

        private int GetChunkY(MazeChunk chunk)
        {
            int index = _mapChunks.IndexOf(chunk);
            if (index < 0) return -1;
            return index / _width;
        }

        public MazeChunk GetChunkFromPosition(Vector3 position)
        {
            foreach (MazeChunk chunk in _mapChunks)
            {
                if (chunk == null) continue;

                Vector3 center = chunk.transform.position + new Vector3((_chunkSize.x / 2f) - (chunk._cellSize / 2), 0, (_chunkSize.y / 2f) - (chunk._cellSize / 2));

                if (position.x >= center.x - _chunkSize.x / 2f &&
                    position.x <= center.x + _chunkSize.x / 2f &&
                    position.z >= center.z - _chunkSize.y / 2f &&
                    position.z <= center.z + _chunkSize.y / 2f)
                    return chunk;
            }
            return null;
        }

    #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_mapChunks == null || _mapChunks.Count == 0) return;

            MazeChunk playerChunk = null;
            var tracker = FindObjectOfType<PlayerChunkTracker>();
            if (Application.isPlaying && tracker != null)
                playerChunk = tracker._currentChunk;

            foreach (MazeChunk chunk in _mapChunks)
            {
                if (chunk == null) continue;

                Vector3 center = chunk.transform.position + new Vector3((_chunkSize.x / 2f) - (chunk._cellSize / 2), 0, (_chunkSize.y / 2f) - (chunk._cellSize / 2));
                Vector3 size = new Vector3(_chunkSize.x, 0.5f, _chunkSize.y);

                Gizmos.color = (chunk == playerChunk) ? new Color(1, 0.5f, 0, 0.35f) : new Color(0, 1, 0, 0.2f);
                Gizmos.DrawCube(center, size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(center, size);
    #if UNITY_EDITOR
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(center + Vector3.up * 2f, $"[{GetChunkX(chunk)}, {GetChunkY(chunk)}]");
    #endif
            }
        }
    #endif

    #endregion
    
    public int GetChunkIndex(MazeChunk chunk)
    {
        return _mapChunks.IndexOf(chunk);
    }

    [ClientRpc]
    public void SetChunkDoorsStateClientRpc(int chunkIndex, bool isOpen)
    {
        if (chunkIndex < 0 || chunkIndex >= _mapChunks.Count)
        {
            return;
        }

        MazeChunk chunk = _mapChunks[chunkIndex];
        if (chunk == null)
        {
            return;
        }

        if (chunk.TryGetComponent(out MazeChunkLabyrinth laby))
        {
            laby.SetDoorsState(isOpen);
        }
    }


}
