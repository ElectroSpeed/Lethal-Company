using System;

public class ConditionNode : Node
{
    private Func<bool> _condition;

    public ConditionNode(Func<bool> condition)
    {
        this._condition = condition;
    }

    public override NodeState Evaluate()
    {
        return _condition.Invoke() ? NodeState.Success : NodeState.Failure;
    }
}