using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyBT : MonoBehaviour
{
    public Transform _player;
    public float _detectionRange = 15f;
    public float _wanderRadius = 20f;
    public float _wanderInterval = 3f;

    private NavMeshAgent _agent;
    private Node _rootNode;
    private float _wanderTimer;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();

        Node playerVisible = new ConditionNode(() => PlayerVisible());
        Node chase = new ActionNode(() => ChasePlayer());
        Node wander = new ActionNode(() => Wander());

        Sequence chaseSequence = new Sequence(new List<Node> { playerVisible, chase });
        _rootNode = new Selector(new List<Node> { chaseSequence, wander });
    }

    void Update()
    {
        _rootNode.Evaluate();
        if (Time.frameCount % 60 == 0)
            Debug.Log("BT running fine at " + Time.frameCount);
    }

    private bool PlayerVisible()
    {
        return Vector3.Distance(transform.position, _player.position) < _detectionRange;
    }

    private NodeState ChasePlayer()
    {
        if (_player == null)
            return NodeState.Failure;

        _agent.destination = _player.position;
        return NodeState.Running;
    }

    private NodeState Wander()
    {
        _wanderTimer += Time.deltaTime;

        if (_wanderTimer >= _wanderInterval || _agent.remainingDistance < 0.5f)
        {
            Vector3 newPos = RandomNavmeshLocation(_wanderRadius);
            _agent.SetDestination(newPos);
            _wanderTimer = 0f;
        }

        return NodeState.Running;
    }
    
    private Vector3 RandomNavmeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
    }
}
