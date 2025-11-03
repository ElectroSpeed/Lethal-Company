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


    [Header("Enemy path")]
    [SerializeField] private List<Vector3> _enemyPath = new();
    private bool _newPosChoosed;

    [Header("Players ref")]
    private List<Player> _players = new();
    [SerializeField] private LayerMask _visionMask;

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

    }

    void Update()
    {
        _rootNode.Evaluate();
    }

    private bool CheckIfPlayerIsTargetable()
    {
        _playerTarget = null;
        float closestDistance = Mathf.Infinity;
        print("Call CheckIfPlayerIsTargetable");

        foreach (Player player in _players)
        {
            print($"try to hit raycast for {_players.Count}");
            if (player == null) continue;

            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (distToPlayer > _detectionRange)
            {
                print($"player was to far");
                continue;
            }

            print($"try to hit raycast on {player.name}");
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, out RaycastHit hit, _detectionRange, _visionMask))
            {
                print($"raycast Hit {hit.collider.name}");
                if (hit.collider.TryGetComponent(out Player hitPlayer) && hitPlayer == player)
                {
                    print($"raycast Hit player, check if is on range");
                    if (distToPlayer < closestDistance)
                    {
                        print($"player is on range, take reference");
                        closestDistance = distToPlayer;
                        _playerTarget = player.transform;
                        return true;
                    }
                }
            }
            return false;
        }
        return false;
    }

    private bool PlayerVisible()
    {
        return CheckIfPlayerIsTargetable();
    }

    private NodeState ChasePlayer()
    {
        //Make sure the enemy follows you thanks to the “marks” left by the player's sprint. 
        if (_playerTarget == null)
            return NodeState.Failure;

        _agent.destination = _playerTarget.position;
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
