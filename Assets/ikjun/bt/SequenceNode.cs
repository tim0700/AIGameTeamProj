using System.Collections.Generic;

public class SequenceNode : BTNode
{
    private List<BTNode> children;

    public SequenceNode(List<BTNode> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate()
    {
        bool anyRunning = false;

        foreach (BTNode child in children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Failure:
                    state = NodeState.Failure;
                    return state;
                case NodeState.Running:
                    anyRunning = true;
                    break;
            }
        }

        state = anyRunning ? NodeState.Running : NodeState.Success;
        return state;
    }
}
