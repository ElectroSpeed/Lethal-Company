using System;

public class ActionNode : Node
{
    private Func<NodeState> _action;
    
    public ActionNode(Func<NodeState> action)
    {
        this._action = action;
    }
    
    public override NodeState Evaluate()
    {
        if (_action == null)
            return NodeState.Failure;

        return _action.Invoke();
    }
}