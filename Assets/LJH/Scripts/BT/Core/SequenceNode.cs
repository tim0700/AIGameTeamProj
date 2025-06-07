using System.Collections.Generic;

namespace LJH.BT
{
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
