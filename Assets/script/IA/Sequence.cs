using System.Collections.Generic;

public class Sequence : Node
{
    private List<Node> _children;

    public Sequence(List<Node> nodes)
    {
        _children = nodes;
    }

    public override NodeState Evaluate()
    {
        bool anyRunning = false;

        foreach (Node node in _children)
        {
            switch (node.Evaluate())
            {
                case NodeState.Failure:
                    _state = NodeState.Failure;
                    return _state;
                case NodeState.Running:
                    anyRunning = true;
                    break;
                case NodeState.Success:
                    continue;
            }
        }

        _state = anyRunning ? NodeState.Running : NodeState.Success;
        return _state;
    }
}