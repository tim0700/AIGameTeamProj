using System.Collections.Generic;

public class SelectorNode : BTNode
{
    private List<BTNode> children;

    public SelectorNode(List<BTNode> children) // 생성자
    {
        this.children = children; // 들어온 자식 노드 리스트를 이 객체의 children 변수에 저장해라 
    }
    public override NodeState Evaluate()
    {
        foreach (BTNode child in children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Success:
                    state = NodeState.Success;
                    return state;
                case NodeState.Running:
                    state = NodeState.Running;
                    return state;
            }
        }

        state = NodeState.Failure;
        return state;
    }
}

