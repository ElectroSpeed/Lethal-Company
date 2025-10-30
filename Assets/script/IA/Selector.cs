using System.Collections.Generic;

public class Selector : Node
{
    private List<Node> _children;

    public Selector(List<Node> nodes)
    {
        _children = nodes;
    }

    public override NodeState Evaluate()
    {
        foreach (Node node in _children)
        {
            switch (node.Evaluate())
            {
                case NodeState.Running:
                    _state = NodeState.Running;
                    return _state;
                case NodeState.Success:
                    _state = NodeState.Success;
                    return _state;
                case NodeState.Failure:
                    continue;
            }
        }
        _state = NodeState.Failure;
        return _state;
    }
}