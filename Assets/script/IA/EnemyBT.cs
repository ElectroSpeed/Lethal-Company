using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBT : MonoBehaviour
{
    public Transform _playerTarget;
    public float _detectionRange = 10f;
    public float _wanderRadius = 2000f;
    public float _wanderInterval = 10f;

    [SerializeField] private NavMeshAgent _agent;
    private Node _rootNode;
    private float _wanderTimer;
    private Vector3 _lastKnownPlayerPos;
    private float _searchTimer;
    private bool _isSearching;
    [SerializeField] private float _searchDurationMin = 10f;
    [SerializeField] private float _searchDurationMax = 30f;
    private float _currentSearchDuration;

    public MapManager _mapManager { private get; set; }

    private List<Vector3> _searchPoints = new();
    private int _currentSearchIndex = 0;

    [Header("Enemy path")]
    [SerializeField] private List<Vector3> _enemyPath = new();
    private bool _newPosChoosed;

    [Header("Players ref")]
    private List<Player> _players = new();
    [SerializeField] private LayerMask _visionMask;

    private bool _isForcedChase = false;


    public void SetEnemyPathOnMap(List<MazeCell> cells)
    {
        _enemyPath.Clear();
        foreach (MazeCell cell in cells)
        {
            _enemyPath.Add(cell.transform.position);
        }
    }

    public void Initialize(MapManager manager)
    {
        _mapManager = manager;
        SetEnemyPathOnMap(_mapManager.GetRandomCellsOnMap());
    }

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();

        Node playerVisible = new ConditionNode(() => PlayerVisible());
        Node chase = new ActionNode(() => ChasePlayer());

        Node hasLastKnown = new ConditionNode(() => HasLastKnownPosition());
        Node search = new ActionNode(() => SearchLastKnownPosition());

        Node wander = new ActionNode(() => Wander());

        Sequence chaseSequence = new Sequence(new List<Node> { playerVisible, chase });
        Sequence searchSequence = new Sequence(new List<Node> { hasLastKnown, search });

        _rootNode = new Selector(new List<Node> { chaseSequence, searchSequence, wander });

        _wanderTimer = _wanderInterval;
        GetComponent<SphereCollider>().radius = _detectionRange;
    }

    void Update()
    {
        if (_isForcedChase && _playerTarget != null)
        {
            _agent.SetDestination(_playerTarget.position);
            return;
        }

        _rootNode.Evaluate();
    }

    #region Player Detection

    private bool CheckIfPlayerIsTargetable()
    {
        _playerTarget = null;

        foreach (Player player in _players)
        {
            if (player == null) continue;

            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (distToPlayer > _detectionRange)
                continue;

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, out RaycastHit hit, _detectionRange, _visionMask))
            {
                if (hit.collider.TryGetComponent(out Player hitPlayer) && hitPlayer == player)
                {
                    _lastKnownPlayerPos = player.transform.position;
                    _isSearching = false;
                    _searchTimer = 0f;
                    _playerTarget = player.transform;
                    return true;
                }
            }
        }

        if (_lastKnownPlayerPos != Vector3.zero && !_isSearching)
        {
            InitializeSearchInChunk(_lastKnownPlayerPos);
        }

        return false;
    }

    private bool PlayerVisible()
    {
        return CheckIfPlayerIsTargetable();
    }

    #endregion

    #region Behavior Tree Nodes

    private NodeState SearchLastKnownPosition()
    {
        if (!_isSearching || _searchPoints.Count == 0)
            return NodeState.Failure;

        _searchTimer += Time.deltaTime;

        if (_searchTimer > _currentSearchDuration)
        {
            _isSearching = false;
            _searchPoints.Clear();
            return NodeState.Failure;
        }

        if (!_agent.pathPending && _agent.remainingDistance < 1f)
        {
            _currentSearchIndex++;

            if (_currentSearchIndex < _searchPoints.Count)
            {
                _agent.SetDestination(_searchPoints[_currentSearchIndex]);
            }
            else
            {
                _currentSearchIndex = 0;
                _agent.SetDestination(_searchPoints[0]);
            }
        }

        return NodeState.Running;
    }

    private bool HasLastKnownPosition()
    {
        return !_isSearching && _lastKnownPlayerPos != Vector3.zero;
    }

    private void InitializeSearchInChunk(Vector3 playerLastPos)
    {
        MazeChunk playerChunk = _mapManager.GetChunkFromWorldPosition(playerLastPos);
        if (playerChunk == null)
        {
            Debug.LogWarning("Aucun chunk trouvé pour la position du joueur.");
            return;
        }

        List<Vector3> randomCells = _mapManager.GetRandomCellsInChunk(playerChunk, 4);

        _searchPoints.Clear();
        foreach (Vector3 cell in randomCells)
        {
            _searchPoints.Add(cell);
        }

        _currentSearchIndex = 0;
        _isSearching = true;
        _currentSearchDuration = Random.Range(_searchDurationMin, _searchDurationMax);
        _searchTimer = 0f;

        if (_searchPoints.Count > 0)
        {
            _enemyPath = randomCells;
        }
    }

    private NodeState ChasePlayer()
    {
        if (_playerTarget == null)
            return NodeState.Failure;

        _isSearching = false;
        _searchPoints.Clear();

        _agent.destination = _playerTarget.position;
        return NodeState.Running;
    }

    private NodeState Wander()
    {
        _wanderTimer += Time.deltaTime;

        if ((_wanderTimer >= _wanderInterval || !_agent.hasPath || _agent.remainingDistance < 0.5f) && !_newPosChoosed)
        {
            Vector3 newPos = RandomNavmeshLocation();

            if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                _newPosChoosed = true;
            }

            _wanderTimer = 0f;
        }

        if (_agent.remainingDistance < 0.5f && !_agent.pathPending)
            _newPosChoosed = false;

        return NodeState.Running;
    }

    #endregion

    #region Forced Chase (Monster Call)

    public void ForceChasePlayer(Transform playerTransform)
    {
        if (playerTransform == null || _agent == null)
            return;

        _playerTarget = playerTransform;
        _isSearching = false;
        _searchPoints.Clear();

        _isForcedChase = true;
        Debug.Log($"{name} est appelé vers {playerTransform.name}");
    }

    public void StopForcedChase()
    {
        _isForcedChase = false;
        _playerTarget = null;
        _agent.ResetPath();
        _isSearching = false;
        _searchPoints.Clear();

        Debug.Log($"{name} a arrêté la chasse forcée et reprend son comportement normal.");
    }

    #endregion

    #region Utility / Triggers

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player))
        {
            if (!_players.Contains(player))
            {
                _players.Add(player);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Player player))
        {
            if (_players.Contains(player))
            {
                _players.Remove(player);
            }
        }
    }

    private Vector3 RandomNavmeshLocation()
    {
        if (_enemyPath.Count <= 0)
        {
            EventBus.Publish(EventType.FillEnemyPath, this);
            return transform.position;
        }

        int randomIndex = Random.Range(0, _enemyPath.Count);
        Vector3 randPos = _enemyPath[randomIndex];

        _enemyPath.Remove(randPos);
        _newPosChoosed = true;

        return randPos;
    }

    #endregion
}
