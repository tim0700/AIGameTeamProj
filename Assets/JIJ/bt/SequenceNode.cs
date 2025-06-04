using System.Collections.Generic;

public class SequenceNode : BTNode
{
    private List<BTNode> children;

    public SequenceNode(List<BTNode> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate() // 현재 노드의 상태를 판단
    {
        bool anyRunning = false;

        foreach (BTNode child in children) // 자식 노드들을 순서대로 Evaluate() (실행)함
        {
            switch (child.Evaluate())
            {
                case NodeState.Failure: // 만약 하나라도 실패(Failure) 하면 바로 이 시퀀스 노드도 실패로 판단하고 리턴
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
