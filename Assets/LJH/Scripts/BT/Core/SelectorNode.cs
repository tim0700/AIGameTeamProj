using System.Collections.Generic;

namespace LJH.BT
{
    /// <summary>
    /// Selector 노드 - 자식 노드들 중 하나라도 성공하면 성공 (OR 논리)
    /// 자식 노드들을 순차적으로 실행하다가 성공하는 노드가 있으면 중단
    /// </summary>
    public class SelectorNode : BTNode
    {
        private List<BTNode> children;

        public SelectorNode(List<BTNode> children)
        {
            this.children = children;
        }

        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            foreach (BTNode child in children)
            {
                child.Initialize(controller);
            }
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            foreach (BTNode child in children)
            {
                switch (child.Evaluate(observation))
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
}
