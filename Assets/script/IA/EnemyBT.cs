using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBT : MonoBehaviour
{
    public Transform _playerTarget;
    public float _detectionRange = 10f;
    public float _wanderRadius = 2000f;
    public float _wanderInterval = 10f;

    private NavMeshAgent _agent;
    private Node _rootNode;
    private float _wanderTimer;
    private bool _isCrossingLink = false;

    [SerializeField] private List<Vector3> _enemyPath = new();
    private bool _newPosChoosed;

    private List<Player> _players = new();
    [SerializeField] private LayerMask _visionMask;
    private bool _navReady;

    public void SetEnemyPathOnMap(List<MazeCell> cells)
    {
        _enemyPath.Clear();
        foreach (MazeCell cell in cells)
        {
            _enemyPath.Add(cell.transform.position);
        }
    }

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        Node playerVisible = new ConditionNode(() => PlayerVisible());
        Node chase = new ActionNode(() => ChasePlayer());
        Node wander = new ActionNode(() => Wander());
        Sequence chaseSequence = new Sequence(new List<Node> { playerVisible, chase });
        _rootNode = new Selector(new List<Node> { chaseSequence, wander });
        _wanderTimer = _wanderInterval;
        GetComponent<SphereCollider>().radius = _detectionRange;
        StartCoroutine(CheckNavMeshReady());
    }

    IEnumerator CheckNavMeshReady()
    {
        while (!_agent.isOnNavMesh)
        {
            yield return null;
        }
        _navReady = true;
    }

    void Update()
    {
        if (!_navReady) return;
        _rootNode.Evaluate();
    }

    private bool CheckIfPlayerIsTargetable()
    {
        _playerTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Player player in _players)
        {
            if (player == null) continue;
            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distToPlayer > _detectionRange) continue;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, out RaycastHit hit, _detectionRange, _visionMask))
            {
                if (hit.collider.TryGetComponent(out Player hitPlayer) && hitPlayer == player)
                {
                    if (distToPlayer < closestDistance)
                    {
                        closestDistance = distToPlayer;
                        _playerTarget = player.transform;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool PlayerVisible()
    {
        return CheckIfPlayerIsTargetable();
    }

    private NodeState ChasePlayer()
    {
        if (_playerTarget == null || !_agent.isOnNavMesh)
            return NodeState.Failure;

        _agent.SetDestination(_playerTarget.position);
        return NodeState.Running;
    }

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

    private NodeState Wander()
    {
        if (!_agent.isOnNavMesh)
            return NodeState.Failure;

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
}
