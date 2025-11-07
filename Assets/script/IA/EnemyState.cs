using UnityEngine;
using UnityEngine.AI;

public class EnemyState : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;

    [Header("DEBUG")]
    public AiState state;

    private void Awake()
    {
        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();
    }

    public void ChangeAIState(AiState state)
    {
        this.state = state;
        switch (state)
        {
            case AiState.Default:
                _agent.speed = 3.5f;
                break;

            case AiState.Angry:
                _agent.speed = Random.Range(4.5f, 5.5f);
                break;

            case AiState.Attack:
                _agent.speed = 10.0f;
                break;

            case AiState.Waiting:
                _agent.speed = 0f;
                break;
        }
    }
}

[System.Serializable]
public enum AiState
{
    Default,
    Angry,
    Attack,
    Waiting,
}
