using System.Collections.Generic;

namespace LJH.BT
{
    /// <summary>
    /// Sequence 노드 - 모든 자식 노드가 성공해야 성공 (AND 논리)
    /// 자식 노드들을 순차적으로 실행하다가 실패하는 노드가 있으면 중단
    /// </summary>
    public class SequenceNode : BTNode
    {
        private List<BTNode> children;

        public SequenceNode(List<BTNode> children)
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
            bool anyRunning = false;

            foreach (BTNode child in children)
            {
                switch (child.Evaluate(observation))
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
}
