using UnityEngine;

public enum NodeState
{
    Running,
    Success,
    Failure
}

public abstract class BTNode
{
    protected NodeState state;

    public NodeState GetState() => state;

    public abstract NodeState Evaluate();
}
